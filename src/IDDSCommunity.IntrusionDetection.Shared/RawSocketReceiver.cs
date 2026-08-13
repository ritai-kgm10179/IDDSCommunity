using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 判斷原始 IP 封包是否需要複製並交付給後續處理程序。
/// </summary>
/// <param name="packet">仍位於共用接收緩衝區內的封包內容。</param>
/// <returns>需要交付時傳回 <see langword="true"/>。</returns>
public delegate bool RawPacketFilter(ReadOnlySpan<byte> packet);

public sealed class RawSocketReceiver : IPacketCaptureReceiver
{
    private const int MaximumPacketSize = 65535;
    private readonly int queueCapacity;
    private readonly RawPacketFilter? packetFilter;
    private Socket? socket;
    private CancellationTokenSource? cancellation;
    private BoundedPacketDispatcher? dispatcher;
    private long subscriberFailureCount;

    public event EventHandler<RawPacketEventArgs>? PacketReceived;
    public event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;
    /// <summary>
    /// 初始化包含界限分發佇列的 Raw Socket 接收器。
    /// </summary>
    /// <param name="queueCapacity">等待訂閱者處理的封包最大數量。</param>
    public RawSocketReceiver(int queueCapacity = 1024, RawPacketFilter? packetFilter = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueCapacity);
        this.queueCapacity = queueCapacity;
        this.packetFilter = packetFilter;
    }
    /// <summary>
    /// 取得 active receive-loop task so callers can supervise its lifetime.
    /// </summary>
    public Task Completion { get; private set; } = Task.CompletedTask;
    /// <summary>
    /// 取得 number of packets offered to the dispatch queue during the current capture.
    /// </summary>
    public long ReceivedPacketCount => dispatcher?.ReceivedCount ?? 0;
    /// <summary>
    /// 取得 number of packets delivered to subscribers during the current capture.
    /// </summary>
    public long DispatchedPacketCount => dispatcher?.DispatchedCount ?? 0;
    /// <summary>
    /// 取得 number of newest packets dropped because the bounded queue was full.
    /// </summary>
    public long DroppedPacketCount => dispatcher?.DroppedCount ?? 0;
    /// <summary>
    /// 取得 number of packet subscriber callbacks that threw an exception.
    /// </summary>
    public long SubscriberFailureCount => Interlocked.Read(ref subscriberFailureCount);
    /// <summary>
    /// 於指定的本機位址啟動 IPv4 封包擷取。
    /// </summary>
    /// <param name="address">要監控的本機 IPv4 位址。</param>
    public void Start(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException(Localization.Strings.Get("Raw packet capture currently requires an IPv4 address."), nameof(address));
        if (socket is not null)
            throw new InvalidOperationException(Localization.Strings.Get("Packet capture has already started."));

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP) { ExclusiveAddressUse = false };
        socket.Bind(new IPEndPoint(address, 0));
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
        socket.IOControl(IOControlCode.ReceiveAll, [3, 0, 0, 0], [3, 0, 0, 0]);
        cancellation = new CancellationTokenSource();
        dispatcher = new BoundedPacketDispatcher(queueCapacity, NotifyPacketReceived);
        Task receiveTask = ReceiveLoopAsync(socket, dispatcher, cancellation.Token);
        Completion = Task.WhenAll(receiveTask, dispatcher.Completion);
    }
    /// <summary>
    /// 停止封包擷取並取消未處理的接收作業。
    /// </summary>
    public void Stop()
    {
        cancellation?.Cancel();
        socket?.Dispose();
        dispatcher?.Complete();
        socket = null;
        cancellation?.Dispose();
        cancellation = null;
    }
    /// <summary>
    /// 釋放通訊埠與取消權杖資源。
    /// </summary>
    public void Dispose() => Stop();

    private async Task ReceiveLoopAsync(Socket activeSocket, BoundedPacketDispatcher packetDispatcher, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MaximumPacketSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int length = await activeSocket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (length <= 0)
                    continue;
                if (packetFilter is not null && !packetFilter(buffer.AsSpan(0, length)))
                    continue;
                byte[] packet = buffer.AsSpan(0, length).ToArray();
                packetDispatcher.TryEnqueue(packet);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            NotifyCaptureFailed(ex);
        }
        finally
        {
            packetDispatcher.Complete();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    /// <summary>
    /// 獨立通知每個封包訂閱者，避免單一故障的消費者影響封包擷取作業。
    /// </summary>
    /// <param name="eventArgs">接收到的封包資料。</param>
    private void NotifyPacketReceived(RawPacketEventArgs eventArgs)
    {
        foreach (EventHandler<RawPacketEventArgs> handler in PacketReceived?.GetInvocationList() ?? [])
        {
            try
            {
                handler(this, eventArgs);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref subscriberFailureCount);
                NotifyCaptureFailed(ex);
            }
        }
    }
    /// <summary>
    /// 發布擷取錯誤，同時避免錯誤觀察者中斷接收迴圈。
    /// </summary>
    /// <param name="exception">擷取作業或訂閱者的例外狀況。</param>
    private void NotifyCaptureFailed(Exception exception)
    {
        foreach (Delegate subscriber in CaptureFailed?.GetInvocationList() ?? [])
        {
            try
            {
                ((EventHandler<RawSocketErrorEventArgs>)subscriber)(this, new RawSocketErrorEventArgs(exception));
            }
            catch (Exception)
            {
                // Error observers must never terminate packet capture.
            }
        }
    }
}

public sealed class RawPacketEventArgs(byte[] packet) : EventArgs
{
    /// <summary>
    /// 取得 immutable-by-contract packet buffer owned by this event instance.
    /// </summary>
    public byte[] Packet { get; } = packet;
}
/// <summary>
/// 描述原始通訊埠封包擷取或訂閱者的失敗狀況。
/// </summary>
/// <param name="exception">中斷處理流程的例外狀況。</param>
public sealed class RawSocketErrorEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// 取得 capture or subscriber exception.
    /// </summary>
    public Exception Exception { get; } = exception;
}

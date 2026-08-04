using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Shared;

public sealed class RawSocketReceiver : IDisposable
{
    private const int MaximumPacketSize = 65535;
    private Socket? socket;
    private CancellationTokenSource? cancellation;

    public event EventHandler<RawPacketEventArgs>? PacketReceived;

    /// <summary>
    /// Starts capturing IPv4 packets on the specified local address.
    /// </summary>
    /// <param name="address">The local IPv4 address to monitor.</param>
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
        _ = ReceiveLoopAsync(socket, cancellation.Token);
    }

    /// <summary>
    /// Stops packet capture and cancels the pending receive operation.
    /// </summary>
    public void Stop()
    {
        cancellation?.Cancel();
        socket?.Dispose();
        socket = null;
        cancellation?.Dispose();
        cancellation = null;
    }

    /// <summary>
    /// Releases the socket and cancellation resources.
    /// </summary>
    public void Dispose() => Stop();

    private async Task ReceiveLoopAsync(Socket activeSocket, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MaximumPacketSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int length = await activeSocket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (length <= 0)
                    continue;
                byte[] packet = buffer.AsSpan(0, length).ToArray();
                PacketReceived?.Invoke(this, new RawPacketEventArgs(packet));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

public sealed class RawPacketEventArgs(byte[] packet) : EventArgs
{
    public ReadOnlyMemory<byte> Packet { get; } = packet;
}

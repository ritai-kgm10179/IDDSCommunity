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
    public event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;

    /// <summary>
    /// Gets the active receive-loop task so callers can supervise its lifetime.
    /// </summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

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
        Completion = ReceiveLoopAsync(socket, cancellation.Token);
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
                NotifyPacketReceived(new RawPacketEventArgs(packet));
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
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Notifies each packet subscriber independently so one faulty consumer cannot stop capture.
    /// </summary>
    /// <param name="eventArgs">The received packet.</param>
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
                NotifyCaptureFailed(ex);
            }
        }
    }

    /// <summary>
    /// Publishes a capture error without allowing an error observer to fault the receive loop.
    /// </summary>
    /// <param name="exception">The capture or subscriber exception.</param>
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
    /// Gets the immutable-by-contract packet buffer owned by this event instance.
    /// </summary>
    public byte[] Packet { get; } = packet;
}

/// <summary>
/// Describes a raw packet capture or subscriber failure.
/// </summary>
/// <param name="exception">The exception that interrupted processing.</param>
public sealed class RawSocketErrorEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// Gets the capture or subscriber exception.
    /// </summary>
    public Exception Exception { get; } = exception;
}

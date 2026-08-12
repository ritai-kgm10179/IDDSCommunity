using System;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal interface IPacketCaptureReceiver : IDisposable
{
    event EventHandler<RawPacketEventArgs>? PacketReceived;
    event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;
    void Start(IPAddress address);
    void Stop();
}

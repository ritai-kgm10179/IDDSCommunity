using System;
using System.Diagnostics;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// 提供協定 Agent 使用的單一共用原始封包擷取轉接器。
/// </summary>
public sealed class PacketSniffer : IDisposable
{
    private IDisposable? subscription;
    private bool paused;

    public event EventHandler? IpPacketReceived;
    public event EventHandler? IpPacketSent;
    /// <summary>
    /// 在收到符合通訊埠條件且已完成解析的 TCP 封包時發生。
    /// </summary>
    public event EventHandler<TcpPacketEventArgs>? TcpPacketReceived;
    /// <summary>
    /// 在送出符合通訊埠條件且已完成解析的 TCP 封包時發生。
    /// </summary>
    public event EventHandler<TcpPacketEventArgs>? TcpPacketSent;
    public event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;

    public int? TcpPort { get; set; }
    public IPAddress IPAddress { get; private set; } = IPAddress.Loopback;

    public void WatchAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        IPAddress = address;
        if (subscription is not null)
            throw new InvalidOperationException(Localization.Strings.Get("Packet capture has already started."));
        subscription = PacketCaptureHub.Subscribe(address, TcpPort, OnPacketSent, OnPacketReceived, eventArgs => OnCaptureFailed(this, eventArgs));
    }

    public void Abort() => paused = true;
    public void Continue() => paused = false;
    public void CloseSocket() => Dispose();

    public void Dispose()
    {
        subscription?.Dispose();
        subscription = null;
    }

    private void OnPacketSent(IPHeader ipHeader, TCPHeader tcpHeader)
    {
        if (paused)
            return;
        TcpPacketSent?.Invoke(this, new TcpPacketEventArgs(ipHeader, tcpHeader));
        IpPacketSent?.Invoke(ipHeader, EventArgs.Empty);
    }

    private void OnPacketReceived(IPHeader ipHeader, TCPHeader tcpHeader)
    {
        if (paused)
            return;
        TcpPacketReceived?.Invoke(this, new TcpPacketEventArgs(ipHeader, tcpHeader));
        IpPacketReceived?.Invoke(ipHeader, EventArgs.Empty);
    }

    internal void DispatchParsedPacketForTest(IPHeader ipHeader, TCPHeader tcpHeader, bool sent)
    {
        if (sent)
            OnPacketSent(ipHeader, tcpHeader);
        else
            OnPacketReceived(ipHeader, tcpHeader);
    }

    private void OnCaptureFailed(object? sender, RawSocketErrorEventArgs eventArgs)
    {
        Trace.TraceError("Packet capture failed on {0}: {1}", IPAddress, eventArgs.Exception);
        CaptureFailed?.Invoke(this, eventArgs);
    }

    public static void LogTrace(Exception exception) => Trace.TraceError("Packet processing failed: {0}", exception);
}

/// <summary>
/// 提供已完成一次解析的 IPv4 TCP 封包。
/// </summary>
public sealed class TcpPacketEventArgs : EventArgs
{
    /// <summary>
    /// 初始化已解析 TCP 封包事件資料的新執行個體。
    /// </summary>
    /// <param name="ipHeader">已解析的 IPv4 標頭。</param>
    /// <param name="tcpHeader">已解析的 TCP 標頭。</param>
    public TcpPacketEventArgs(IPHeader ipHeader, TCPHeader tcpHeader)
    {
        ArgumentNullException.ThrowIfNull(ipHeader);
        ArgumentNullException.ThrowIfNull(tcpHeader);
        IpHeader = ipHeader;
        TcpHeader = tcpHeader;
    }

    /// <summary>
    /// 取得 IPv4 標頭與承載資料。
    /// </summary>
    public IPHeader IpHeader { get; }

    /// <summary>
    /// 取得 TCP 標頭與承載資料。
    /// </summary>
    public TCPHeader TcpHeader { get; }
}

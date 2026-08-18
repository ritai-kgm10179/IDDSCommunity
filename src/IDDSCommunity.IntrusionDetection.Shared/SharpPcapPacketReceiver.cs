using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal sealed class SharpPcapPacketReceiver(LibPcapLiveDevice device, string filter, int queueCapacity = 1024) : IPacketCaptureReceiver
{
    private readonly LibPcapLiveDevice device = device ?? throw new ArgumentNullException(nameof(device));
    private readonly string filter = string.IsNullOrWhiteSpace(filter) ? throw new ArgumentException(null, nameof(filter)) : filter;
    private readonly int queueCapacity = queueCapacity > 0 ? queueCapacity : throw new ArgumentOutOfRangeException(nameof(queueCapacity));
    private BoundedPacketDispatcher? dispatcher;
    private bool stopping;

        /// <summary>
    /// 當 PacketReceived 時引發之事件。
    /// </summary>
public event EventHandler<RawPacketEventArgs>? PacketReceived;
        /// <summary>
    /// 當 CaptureFailed 時引發之事件。
    /// </summary>
public event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;

    internal static bool TryCreate(IPAddress address, string filter, out SharpPcapPacketReceiver? receiver)
    {
        ArgumentNullException.ThrowIfNull(address);
        receiver = null;
        try
        {
            LibPcapLiveDevice? device = CaptureDeviceList.Instance
                .OfType<LibPcapLiveDevice>()
                .FirstOrDefault(candidate => candidate.Addresses.Any(item => string.Equals(item.Addr?.ToString(), address.ToString(), StringComparison.OrdinalIgnoreCase)));
            if (device is null)
                return false;
            receiver = new SharpPcapPacketReceiver(device, filter);
            return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or TypeInitializationException or PcapException)
        {
            return false;
        }
    }

    /// <summary>
    /// 透過 Npcap/WinPcap 介面在指定 IP 位址上啟動封包擷取。
    /// </summary>
        /// <param name="address">欲監聽之本機網路介面 IP 位址。</param>
    public void Start(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (dispatcher is not null)
            throw new InvalidOperationException();

        stopping = false;
        dispatcher = new BoundedPacketDispatcher(queueCapacity, DispatchToSubscribers);
        device.OnPacketArrival += OnPacketArrival;
        device.OnCaptureStopped += OnCaptureStopped;
        try
        {
            device.Open(new DeviceConfiguration
            {
                Mode = DeviceModes.None,
                ReadTimeout = 500,
                Snaplen = 65535,
                Immediate = false,
                KernelBufferSize = 1_048_576
            });
            device.Filter = filter;
            device.StartCapture();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    /// <summary>
    /// 停止封包擷取裝置並關閉工作階段。
    /// </summary>
    public void Stop()
    {
        if (stopping)
            return;
        stopping = true;
        device.OnPacketArrival -= OnPacketArrival;
        device.OnCaptureStopped -= OnCaptureStopped;
        try
        {
            if (device.Started)
                device.StopCapture();
        }
        catch (PcapException)
        {
        }
        try
        {
            if (device.Opened)
                device.Close();
        }
        catch (PcapException)
        {
        }
        dispatcher?.Complete();
        dispatcher = null;
    }

    /// <summary>
    /// 釋放 SharpPcap 接收器與相關驅動程式連線資源。
    /// </summary>
    public void Dispose() => Stop();

    /// <summary>
    /// 獨立通知每個封包訂閱者，避免單一故障的消費者影響其餘訂閱者或中止整個擷取管線；
    /// 與 <see cref="RawSocketReceiver"/> 的 NotifyPacketReceived 具備對等的例外隔離行為。
    /// </summary>
    /// <param name="packet">接收到的封包資料。</param>
    private void DispatchToSubscribers(RawPacketEventArgs packet)
    {
        foreach (EventHandler<RawPacketEventArgs> handler in PacketReceived?.GetInvocationList() ?? [])
        {
            try
            {
                handler(this, packet);
            }
            catch (Exception exception)
            {
                Trace.TraceError("SharpPcap packet subscriber failed on {0}: {1}", device.Name, exception.Message);
            }
        }
    }

    private void OnPacketArrival(object sender, PacketCapture capture)
    {
        ReadOnlySpan<byte> data = capture.Data;
        if (!TryGetIpPacket((int)capture.Device.LinkType, data, out ReadOnlySpan<byte> packet))
            return;
        dispatcher?.TryEnqueue(packet.ToArray());
    }

    private void OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
    {
        if (!stopping && status == CaptureStoppedEventStatus.ErrorWhileCapturing)
            CaptureFailed?.Invoke(this, new RawSocketErrorEventArgs(new InvalidOperationException()));
    }

    internal static bool TryGetIpPacket(int linkType, ReadOnlySpan<byte> frame, out ReadOnlySpan<byte> packet)
    {
        packet = default;
        int offset;
        if (linkType is (int)LinkLayers.Raw or (int)LinkLayers.RawLegacy)
        {
            offset = 0;
        }
        else if (linkType is (int)LinkLayers.Null or (int)LinkLayers.Loop)
        {
            offset = 4;
        }
        else if (linkType == (int)LinkLayers.Ethernet)
        {
            if (frame.Length < 14)
                return false;
            offset = 14;
            ushort etherType = (ushort)((frame[12] << 8) | frame[13]);
            while (etherType is 0x8100 or 0x88A8)
            {
                if (frame.Length < offset + 4)
                    return false;
                etherType = (ushort)((frame[offset + 2] << 8) | frame[offset + 3]);
                offset += 4;
            }
            if (etherType != 0x0800)
                return false;
        }
        else
        {
            return false;
        }
        if (frame.Length <= offset || frame[offset] >> 4 != 4)
            return false;
        packet = frame[offset..];
        return true;
    }
}

using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class PacketSnifferTest
{
    /// <summary>
    /// 驗證最佳化事件會重用集中解析完成的 IP 與 TCP 標頭。
    /// </summary>
    [TestMethod]
    public void ParsedTcpEvent_ReusesParsedHeadersWithoutAgentReparse()
    {
        byte[] packet = CreateIPv4TcpPacket();
        IPHeader ipHeader = new(packet, packet.Length);
        TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);
        PacketSniffer sniffer = new();
        TcpPacketEventArgs? received = null;
        sniffer.TcpPacketReceived += (_, eventArgs) => received = eventArgs;

        sniffer.DispatchParsedPacketForTest(ipHeader, tcpHeader, sent: false);

        Assert.IsNotNull(received);
        Assert.AreSame(ipHeader, received.IpHeader);
        Assert.AreSame(tcpHeader, received.TcpHeader);
    }

    /// <summary>
    /// 驗證 Pcap Filter 僅擷取指定 IPv4、TCP 與已啟用的服務通訊埠。
    /// </summary>
    [TestMethod]
    public void PcapFilter_RestrictsAddressProtocolAndPorts()
    {
        string filter = PacketCaptureHub.BuildPcapFilter(IPAddress.Parse("192.0.2.10"), [25, 21, 25, null]);

        Assert.AreEqual("ip and host 192.0.2.10 and tcp and (port 21 or port 25)", filter);
    }

    /// <summary>
    /// 驗證 Pcap Ethernet Frame 會移除資料鏈結標頭後再交付 IPv4 封包。
    /// </summary>
    [TestMethod]
    public void PcapEthernetFrame_ExtractsIPv4Packet()
    {
        byte[] frame = new byte[54];
        frame[12] = 0x08;
        frame[13] = 0x00;
        frame[14] = 0x45;

        bool parsed = SharpPcapPacketReceiver.TryGetIpPacket(1, frame, out ReadOnlySpan<byte> packet);

        Assert.IsTrue(parsed);
        Assert.AreEqual(40, packet.Length);
        Assert.AreEqual(0x45, packet[0]);
    }

    /// <summary>
    /// 驗證 Pcap Loopback Frame 會移除四位元組家族標頭。
    /// </summary>
    [TestMethod]
    public void PcapLoopbackFrame_ExtractsIPv4Packet()
    {
        byte[] frame = new byte[44];
        frame[4] = 0x45;

        bool parsed = SharpPcapPacketReceiver.TryGetIpPacket(0, frame, out ReadOnlySpan<byte> packet);

        Assert.IsTrue(parsed);
        Assert.AreEqual(40, packet.Length);
    }

    /// <summary>
    /// 驗證 Pcap Filter 針對 IPv6 位址產生正確的 ip6 BPF 過濾表達式。
    /// </summary>
    [TestMethod]
    public void PcapFilter_IPv6_GeneratesIp6Filter()
    {
        string filter = PacketCaptureHub.BuildPcapFilter(IPAddress.Parse("2001:db8::1"), [80, 443]);

        Assert.AreEqual("ip6 and host 2001:db8::1 and tcp and (port 80 or port 443)", filter);
    }

    /// <summary>
    /// 驗證 Pcap Ethernet Frame 支援識別 EtherType 0x86DD 並提取 IPv6 封包。
    /// </summary>
    [TestMethod]
    public void PcapEthernetFrame_ExtractsIPv6Packet()
    {
        byte[] frame = new byte[74];
        frame[12] = 0x86;
        frame[13] = 0xDD;
        frame[14] = 0x60;

        bool parsed = SharpPcapPacketReceiver.TryGetIpPacket(1, frame, out ReadOnlySpan<byte> packet);

        Assert.IsTrue(parsed);
        Assert.AreEqual(60, packet.Length);
        Assert.AreEqual(0x60, packet[0]);
    }

    /// <summary>
    /// 驗證 IPHeader 正確解析 IPv6 標頭、通訊協定、來源與目的地 IP。
    /// </summary>
    [TestMethod]
    public void IPHeader_IPv6_ParsesCorrectly()
    {
        byte[] packet = new byte[60];
        packet[0] = 0x60; // Version 6
        packet[4] = 0;
        packet[5] = 20; // Payload Length: 20 bytes
        packet[6] = 6;  // Next Header: TCP
        packet[7] = 64; // Hop Limit
        byte[] src = IPAddress.Parse("2001:db8::1").GetAddressBytes();
        byte[] dst = IPAddress.Parse("2001:db8::2").GetAddressBytes();
        src.CopyTo(packet, 8);
        dst.CopyTo(packet, 24);
        // TCP ports
        packet[40] = 0x30;
        packet[41] = 0x39; // 12345
        packet[42] = 0x00;
        packet[43] = 0x50; // 80
        packet[52] = 0x50; // Data offset 5 * 4 = 20

        bool parsed = IPHeader.TryParse(packet, packet.Length, out IPHeader? ipHeader);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(ipHeader);
        Assert.AreEqual("IP v6", ipHeader.Version);
        Assert.AreEqual(Protocol.Tcp, ipHeader.ProtocolType);
        Assert.AreEqual(IPAddress.Parse("2001:db8::1"), ipHeader.SourceAddress);
        Assert.AreEqual(IPAddress.Parse("2001:db8::2"), ipHeader.DestinationAddress);
        Assert.AreEqual(20, ipHeader.MessageLength);
    }

    private static byte[] CreateIPv4TcpPacket()
    {
        byte[] packet = new byte[40];
        packet[0] = 0x45;
        packet[3] = 40;
        packet[8] = 64;
        packet[9] = 6;
        packet[12] = 192;
        packet[13] = 0;
        packet[14] = 2;
        packet[15] = 1;
        packet[16] = 198;
        packet[17] = 51;
        packet[18] = 100;
        packet[19] = 2;
        packet[20] = 0x30;
        packet[21] = 0x39;
        packet[22] = 0x01;
        packet[23] = 0xBB;
        packet[32] = 0x50;
        return packet;
    }
}

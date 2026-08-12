using System;
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

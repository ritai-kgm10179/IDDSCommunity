using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class PacketHeaderTest
{
    /// <summary>
    /// Verifies that a representative IPv4 TCP packet preserves its addresses, ports, and payload.
    /// </summary>
    [TestMethod]
    public void IPv4TcpPacket_ParsesAddressesPortsAndPayload()
    {
        byte[] packet = CreateIPv4TcpPacket([0x41, 0x42, 0x43]);

        IPHeader ipHeader = new(packet, packet.Length);
        TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);

        Assert.AreEqual("IP v4", ipHeader.Version);
        Assert.AreEqual("192.0.2.1", ipHeader.SourceAddress.ToString());
        Assert.AreEqual("198.51.100.2", ipHeader.DestinationAddress.ToString());
        Assert.AreEqual(Protocol.Tcp, ipHeader.ProtocolType);
        Assert.AreEqual("12345", tcpHeader.SourcePort);
        Assert.AreEqual("443", tcpHeader.DestinationPort);
        CollectionAssert.AreEqual(new byte[] { 0x41, 0x42, 0x43 }, tcpHeader.Data[..tcpHeader.MessageLength]);
    }
    /// <summary>
    /// Verifies that a truncated IPv4 packet never copies beyond the received input.
    /// </summary>
    [TestMethod]
    public void IPv4Packet_WithTruncatedPayload_IsRejected()
    {
        byte[] packet = CreateIPv4TcpPacket([0x41, 0x42, 0x43]);

        Assert.IsFalse(IPHeader.TryParse(packet, 24, out IPHeader? header));
        Assert.IsNull(header);
    }
    /// <summary>
    /// Verifies that a TCP header without application data reports an empty payload.
    /// </summary>
    [TestMethod]
    public void TcpPacket_WithHeaderOnly_HasNoPayload()
    {
        byte[] packet = CreateIPv4TcpPacket([]);
        IPHeader ipHeader = new(packet, packet.Length);

        TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);

        Assert.AreEqual((ushort)0, tcpHeader.MessageLength);
    }
    /// <summary>
    /// 驗證大型 TCP 承載資料不受固定大小緩衝區限制。
    /// </summary>
    [TestMethod]
    public void TcpPacket_WithLargePayload_PreservesEntirePayload()
    {
        byte[] payload = new byte[4096];
        Random.Shared.NextBytes(payload);
        byte[] packet = CreateIPv4TcpPacket(payload);

        Assert.IsTrue(IPHeader.TryParse(packet, packet.Length, out IPHeader? ipHeader));
        Assert.IsNotNull(ipHeader);
        Assert.IsTrue(TCPHeader.TryParse(ipHeader.Data, ipHeader.MessageLength, out TCPHeader? tcpHeader));
        Assert.IsNotNull(tcpHeader);

        Assert.AreEqual(payload.Length, tcpHeader.MessageLength);
        CollectionAssert.AreEqual(payload, tcpHeader.Data);
    }
    /// <summary>
    /// 驗證截斷與無效標頭會回報解析失敗，而不會擲回例外狀況。
    /// </summary>
    [TestMethod]
    public void TryParse_WithMalformedPackets_ReturnsFalseWithoutThrowing()
    {
        byte[] truncatedIp = new byte[19];
        byte[] invalidTcp = new byte[20];

        Assert.IsFalse(IPHeader.TryParse(truncatedIp, truncatedIp.Length, out IPHeader? ipHeader));
        Assert.IsNull(ipHeader);
        Assert.IsFalse(TCPHeader.TryParse(invalidTcp, invalidTcp.Length, out TCPHeader? tcpHeader));
        Assert.IsNull(tcpHeader);
    }
    /// <summary>
    /// 驗證只讀取數值通訊埠不會具現化 IP 或 TCP 承載資料陣列。
    /// </summary>
    [TestMethod]
    public void NumericPorts_DoNotRequirePayloadMaterialization()
    {
        byte[] packet = CreateIPv4TcpPacket(new byte[1024]);
        long before = GC.GetAllocatedBytesForCurrentThread();

        Assert.IsTrue(IPHeader.TryParse(packet, packet.Length, out IPHeader? ipHeader));
        Assert.IsNotNull(ipHeader);
        Assert.IsTrue(TCPHeader.TryParse(ipHeader.Payload, out TCPHeader? tcpHeader));
        Assert.IsNotNull(tcpHeader);
        Assert.AreEqual((ushort)12345, tcpHeader.SourcePortValue);
        Assert.AreEqual((ushort)443, tcpHeader.DestinationPortValue);

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsLessThan(512L, allocated);
    }
    /// <summary>
    /// Creates a deterministic IPv4 TCP packet for parser characterization tests.
    /// </summary>
    /// <param name="payload">The TCP application payload.</param>
    /// <returns>傳回 encoded IPv4 packet 的結果。</returns>
    private static byte[] CreateIPv4TcpPacket(byte[] payload)
    {
        byte[] packet = new byte[40 + payload.Length];
        packet[0] = 0x45;
        packet[2] = (byte)(packet.Length >> 8);
        packet[3] = (byte)packet.Length;
        packet[6] = 0x40;
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
        packet[33] = 0x12;
        packet[34] = 0x20;
        Array.Copy(payload, 0, packet, 40, payload.Length);
        return packet;
    }
}

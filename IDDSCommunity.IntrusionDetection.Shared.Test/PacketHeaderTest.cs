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
    public void IPv4Packet_WithTruncatedPayload_DoesNotCopyPastInput()
    {
        byte[] packet = CreateIPv4TcpPacket([0x41, 0x42, 0x43]);

        IPHeader header = new(packet, 24);

        Assert.AreEqual(23, header.MessageLength);
        CollectionAssert.AreEqual(new byte[23], header.Data);
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

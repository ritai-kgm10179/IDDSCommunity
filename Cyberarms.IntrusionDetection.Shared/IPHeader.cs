using System;
using System.IO;
using System.Net;

namespace Cyberarms.IntrusionDetection.Shared;

public enum Protocol
{
    Tcp = 6,
    Udp = 17,
    Tlsp = 56,
    Unknown = -1
}

public class IPHeader
{
    private byte byVersionAndHeaderLength;
    private byte byDifferentiatedServices;
    private ushort usTotalLength;
    private ushort usIdentification;
    private ushort usFlagsAndOffset;
    private byte byTTL;
    private byte byProtocol;
    private short sChecksum;
    private uint uiSourceIPAddress;
    private uint uiDestinationIPAddress;

    private byte byHeaderLength;
    private byte[] byIPData = [];

    public IPHeader(byte[] byBuffer, int nReceived)
    {
        try
        {
            using MemoryStream memoryStream = new(byBuffer, 0, nReceived);
            using BinaryReader binaryReader = new(memoryStream);

            byVersionAndHeaderLength = binaryReader.ReadByte();
            byDifferentiatedServices = binaryReader.ReadByte();
            usTotalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
            usIdentification = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
            usFlagsAndOffset = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
            byTTL = binaryReader.ReadByte();
            byProtocol = binaryReader.ReadByte();
            sChecksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
            uiSourceIPAddress = (uint)binaryReader.ReadInt32();
            uiDestinationIPAddress = (uint)binaryReader.ReadInt32();

            byHeaderLength = (byte)((byVersionAndHeaderLength & 0x0F) * 4);

            int dataLength = Math.Max(0, usTotalLength - byHeaderLength);
            byIPData = new byte[dataLength];

            if (dataLength > 0 && nReceived >= byHeaderLength + dataLength)
            {
                Array.Copy(byBuffer, byHeaderLength, byIPData, 0, dataLength);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    public string Version => (byVersionAndHeaderLength >> 4) switch
    {
        4 => "IP v4",
        6 => "IP v6",
        _ => "Unknown"
    };

    public string HeaderLength => byHeaderLength.ToString();

    public ushort MessageLength => (ushort)Math.Max(0, usTotalLength - byHeaderLength);

    public string DifferentiatedServices => $"0x{byDifferentiatedServices:x2} ({byDifferentiatedServices})";

    public string Flags => (usFlagsAndOffset >> 13) switch
    {
        2 => "Don't fragment",
        1 => "More fragments to come",
        var n => n.ToString()
    };

    public string FragmentationOffset => ((usFlagsAndOffset << 3) >> 3).ToString();

    public string TTL => byTTL.ToString();

    public Protocol ProtocolType => byProtocol switch
    {
        6 => Protocol.Tcp,
        17 => Protocol.Udp,
        56 => Protocol.Tlsp,
        _ => Protocol.Unknown
    };

    public string Checksum => $"0x{sChecksum:x2}";

    public IPAddress SourceAddress => new(uiSourceIPAddress);

    public IPAddress DestinationAddress => new(uiDestinationIPAddress);

    public string TotalLength => usTotalLength.ToString();

    public string Identification => usIdentification.ToString();

    public byte[] Data => byIPData;
}

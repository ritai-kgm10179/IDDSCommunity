using System;
using System.Net;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class TCPHeader
{
    private readonly ushort usSourcePort;
    private readonly ushort usDestinationPort;
    private readonly uint uiSequenceNumber = 555;
    private readonly uint uiAcknowledgementNumber = 555;
    private readonly ushort usDataOffsetAndFlags = 555;
    private readonly ushort usWindow = 555;
    private readonly short sChecksum = 555;
    private readonly ushort usUrgentPointer;

    private readonly byte byHeaderLength;
    private readonly ushort usMessageLength;
    private readonly byte[] byTCPData = new byte[128];
    /// <summary>
    /// 初始化 <see cref="TCPHeader"/> class的新執行個體。
    /// </summary>
    /// <param name="byBuffer">by buffer參數。</param>
    /// <param name="nReceived">n received參數。</param>

    public TCPHeader(byte[] byBuffer, int nReceived)
    {
        using MemoryStream memoryStream = new(byBuffer, 0, nReceived);
        using BinaryReader binaryReader = new(memoryStream);

        usSourcePort = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        usDestinationPort = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        uiSequenceNumber = (uint)IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
        uiAcknowledgementNumber = (uint)IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
        usDataOffsetAndFlags = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        usWindow = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        sChecksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
        usUrgentPointer = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

        byHeaderLength = (byte)(usDataOffsetAndFlags >> 12);
        byHeaderLength *= 4;

        usMessageLength = (ushort)(nReceived - byHeaderLength);
        Array.Copy(byBuffer, byHeaderLength, byTCPData, 0, nReceived - byHeaderLength);
    }

    public string SourcePort => usSourcePort.ToString();
    public string DestinationPort => usDestinationPort.ToString();
    public string SequenceNumber => uiSequenceNumber.ToString();

    public string AcknowledgementNumber => (usDataOffsetAndFlags & 0x10) != 0 ? uiAcknowledgementNumber.ToString() : string.Empty;

    public string HeaderLength => byHeaderLength.ToString();
    public string WindowSize => usWindow.ToString();

    public string UrgentPointer => (usDataOffsetAndFlags & 0x20) != 0 ? usUrgentPointer.ToString() : string.Empty;

    public string Flags
    {
        get
        {
            int nFlags = usDataOffsetAndFlags & 0x3F;
            string strFlags = $"0x{nFlags:x2} (";

            if ((nFlags & 0x01) != 0) strFlags += "FIN, ";
            if ((nFlags & 0x02) != 0) strFlags += "SYN, ";
            if ((nFlags & 0x04) != 0) strFlags += "RST, ";
            if ((nFlags & 0x08) != 0) strFlags += "PSH, ";
            if ((nFlags & 0x10) != 0) strFlags += "ACK, ";
            if ((nFlags & 0x20) != 0) strFlags += "URG";

            strFlags += ")";

            if (strFlags.Contains("()"))
            {
                strFlags = strFlags[..^3];
            }
            else if (strFlags.Contains(", )"))
            {
                strFlags = strFlags.Remove(strFlags.Length - 3, 2);
            }

            return strFlags;
        }
    }

    public string Checksum => $"0x{sChecksum:x2}";
    public byte[] Data => byTCPData;
    public ushort MessageLength => usMessageLength;
}

using System;
using System.Buffers.Binary;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class TCPHeader
{
    private ushort sourcePort;
    private ushort destinationPort;
    private uint sequenceNumber;
    private uint acknowledgementNumber;
    private ushort dataOffsetAndFlags;
    private ushort window;
    private short checksum;
    private ushort urgentPointer;
    private byte headerLength;
    private ReadOnlyMemory<byte> payload;
    private byte[]? materializedPayload;

    /// <summary>
    /// 初始化 TCP 標頭的新執行個體；格式錯誤或資料截斷時建立無效標頭而不擲回例外狀況。
    /// </summary>
    /// <param name="buffer">包含 TCP 區段的緩衝區。</param>
    /// <param name="received">緩衝區內實際收到的位元組數量。</param>
    public TCPHeader(byte[] buffer, int received) => IsValid = TryInitialize(buffer, received);

    private TCPHeader(ReadOnlyMemory<byte> segment) => IsValid = TryInitialize(segment);

    /// <summary>
    /// 嘗試解析完整 TCP 區段。
    /// </summary>
    /// <param name="buffer">包含 TCP 區段的緩衝區。</param>
    /// <param name="received">緩衝區內實際收到的位元組數量。</param>
    /// <param name="header">解析成功時的 TCP 標頭。</param>
    /// <returns>若區段完整且格式有效則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryParse(byte[] buffer, int received, out TCPHeader? header)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (received < 0 || received > buffer.Length)
        {
            header = null;
            return false;
        }
        return TryParse(buffer.AsMemory(0, received), out header);
    }

    internal static bool TryParse(ReadOnlyMemory<byte> segment, out TCPHeader? header)
    {
        TCPHeader candidate = new(segment);
        header = candidate.IsValid ? candidate : null;
        return candidate.IsValid;
    }

    /// <summary>
    /// 取得標頭是否通過 TCP 長度與格式驗證。
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 取得來源通訊埠數值。
    /// </summary>
    public ushort SourcePortValue => sourcePort;

    /// <summary>
    /// 取得目的通訊埠數值。
    /// </summary>
    public ushort DestinationPortValue => destinationPort;

    public string SourcePort => sourcePort.ToString();
    public string DestinationPort => destinationPort.ToString();
    public string SequenceNumber => sequenceNumber.ToString();

    public string AcknowledgementNumber => (dataOffsetAndFlags & 0x10) != 0 ? acknowledgementNumber.ToString() : string.Empty;

    public string HeaderLength => headerLength.ToString();
    public string WindowSize => window.ToString();

    public string UrgentPointer => (dataOffsetAndFlags & 0x20) != 0 ? urgentPointer.ToString() : string.Empty;

    public string Flags
    {
        get
        {
            int flags = dataOffsetAndFlags & 0x3F;
            string result = $"0x{flags:x2} (";
            if ((flags & 0x01) != 0) result += "FIN, ";
            if ((flags & 0x02) != 0) result += "SYN, ";
            if ((flags & 0x04) != 0) result += "RST, ";
            if ((flags & 0x08) != 0) result += "PSH, ";
            if ((flags & 0x10) != 0) result += "ACK, ";
            if ((flags & 0x20) != 0) result += "URG";
            result += ")";
            if (result.Contains("()")) return result[..^3];
            return result.Contains(", )") ? result.Remove(result.Length - 3, 2) : result;
        }
    }

    public string Checksum => $"0x{checksum:x2}";
    public byte[] Data => materializedPayload ??= payload.ToArray();
    public ushort MessageLength => (ushort)payload.Length;

    private bool TryInitialize(byte[] buffer, int received)
    {
        if (received < 0 || received > buffer.Length)
            return false;
        return TryInitialize(buffer.AsMemory(0, received));
    }

    private bool TryInitialize(ReadOnlyMemory<byte> segment)
    {
        if (segment.Length < 20)
            return false;
        ReadOnlySpan<byte> bytes = segment.Span;
        int candidateHeaderLength = (bytes[12] >> 4) * 4;
        if (candidateHeaderLength < 20 || candidateHeaderLength > segment.Length)
            return false;

        sourcePort = BinaryPrimitives.ReadUInt16BigEndian(bytes);
        destinationPort = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
        sequenceNumber = BinaryPrimitives.ReadUInt32BigEndian(bytes[4..]);
        acknowledgementNumber = BinaryPrimitives.ReadUInt32BigEndian(bytes[8..]);
        dataOffsetAndFlags = BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]);
        window = BinaryPrimitives.ReadUInt16BigEndian(bytes[14..]);
        checksum = BinaryPrimitives.ReadInt16BigEndian(bytes[16..]);
        urgentPointer = BinaryPrimitives.ReadUInt16BigEndian(bytes[18..]);
        headerLength = (byte)candidateHeaderLength;
        payload = segment[candidateHeaderLength..];
        return true;
    }
}

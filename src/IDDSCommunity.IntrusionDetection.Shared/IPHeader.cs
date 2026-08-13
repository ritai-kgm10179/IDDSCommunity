using System;
using System.Buffers.Binary;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared;

public enum Protocol
{
    Tcp = 6,
    Udp = 17,
    Tlsp = 56,
    Unknown = -1
}

public class IPHeader
{
    private byte versionAndHeaderLength;
    private byte differentiatedServices;
    private ushort totalLength;
    private ushort identification;
    private ushort flagsAndOffset;
    private byte ttl;
    private byte protocol;
    private short checksum;
    private uint sourceIpAddress;
    private uint destinationIpAddress;
    private byte headerLength;
    private ReadOnlyMemory<byte> payload;
    private byte[]? materializedPayload;

    /// <summary>
    /// 初始化 IPv4 標頭的新執行個體；格式錯誤或資料截斷時建立無效標頭而不擲回例外狀況。
    /// </summary>
    /// <param name="buffer">包含 IPv4 封包的緩衝區。</param>
    /// <param name="received">緩衝區內實際收到的位元組數量。</param>
    public IPHeader(byte[] buffer, int received) => IsValid = TryInitialize(buffer, received);

    /// <summary>
    /// 嘗試解析完整 IPv4 封包。
    /// </summary>
    /// <param name="buffer">包含 IPv4 封包的緩衝區。</param>
    /// <param name="received">緩衝區內實際收到的位元組數量。</param>
    /// <param name="header">解析成功時的 IPv4 標頭。</param>
    /// <returns>若封包完整且格式有效則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryParse(byte[] buffer, int received, out IPHeader? header)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        IPHeader candidate = new(buffer, received);
        header = candidate.IsValid ? candidate : null;
        return candidate.IsValid;
    }

    /// <summary>
    /// 取得標頭是否通過 IPv4 長度與格式驗證。
    /// </summary>
    public bool IsValid { get; }

    public string Version => (versionAndHeaderLength >> 4) switch
    {
        4 => "IP v4",
        6 => "IP v6",
        _ => "Unknown"
    };

    public string HeaderLength => headerLength.ToString();

    public ushort MessageLength => (ushort)payload.Length;

    public string DifferentiatedServices => $"0x{differentiatedServices:x2} ({differentiatedServices})";

    public string Flags => (flagsAndOffset >> 13) switch
    {
        2 => "Don't fragment",
        1 => "More fragments to come",
        var value => value.ToString()
    };

    public string FragmentationOffset => ((flagsAndOffset << 3) >> 3).ToString();

    public string TTL => ttl.ToString();

    public Protocol ProtocolType => protocol switch
    {
        6 => Protocol.Tcp,
        17 => Protocol.Udp,
        56 => Protocol.Tlsp,
        _ => Protocol.Unknown
    };

    public string Checksum => $"0x{checksum:x2}";

    public IPAddress SourceAddress => new(sourceIpAddress);

    public IPAddress DestinationAddress => new(destinationIpAddress);

    public string TotalLength => totalLength.ToString();

    public string Identification => identification.ToString();

    public byte[] Data => materializedPayload ??= payload.ToArray();

    internal ReadOnlyMemory<byte> Payload => payload;

    private bool TryInitialize(byte[] buffer, int received)
    {
        if (received < 20 || received > buffer.Length)
            return false;
        ReadOnlySpan<byte> packet = buffer.AsSpan(0, received);
        byte candidateVersionAndLength = packet[0];
        int version = candidateVersionAndLength >> 4;
        int candidateHeaderLength = (candidateVersionAndLength & 0x0F) * 4;
        ushort candidateTotalLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        if (version != 4 || candidateHeaderLength < 20 || candidateHeaderLength > received || candidateTotalLength < candidateHeaderLength || candidateTotalLength > received)
            return false;

        versionAndHeaderLength = candidateVersionAndLength;
        differentiatedServices = packet[1];
        totalLength = candidateTotalLength;
        identification = BinaryPrimitives.ReadUInt16BigEndian(packet[4..]);
        flagsAndOffset = BinaryPrimitives.ReadUInt16BigEndian(packet[6..]);
        ttl = packet[8];
        protocol = packet[9];
        checksum = BinaryPrimitives.ReadInt16BigEndian(packet[10..]);
        sourceIpAddress = BinaryPrimitives.ReadUInt32LittleEndian(packet[12..]);
        destinationIpAddress = BinaryPrimitives.ReadUInt32LittleEndian(packet[16..]);
        headerLength = (byte)candidateHeaderLength;
        payload = buffer.AsMemory(candidateHeaderLength, candidateTotalLength - candidateHeaderLength);
        return true;
    }
}

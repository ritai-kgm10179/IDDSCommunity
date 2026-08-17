using System;
using System.IO;

namespace IDDSCommunity.Agents.TerminalServer;

/// <summary>
/// 解析單一 TLS/SSL 記錄層封包標頭。
/// </summary>
public class AppLayerTlsSsl
{

    /// <summary>
    /// TLS 應用程式資料內容類型。
    /// </summary>
    public const byte CONTENT_TYPE_SSL_APPLICATION_DATA = 0x17;
    /// <summary>
    /// TLS 加密警示內容類型。
    /// </summary>
    public const byte CONTENT_TYPE_ENCRYPTED_ALERT = 0x15;
    /// <summary>
    /// TLS 交握內容類型。
    /// </summary>
    public const byte CONTENT_TYPE_HANDSHAKE = 0x16;

    /// <summary>
    /// 表示已解析之 TLS 記錄層通訊協定標頭。
    /// </summary>
    public struct TlsProtocolHeader
    {
        /// <summary>
        /// 內容類型。
        /// </summary>
        public byte ContentType;
        /// <summary>
        /// 主要版本號。
        /// </summary>
        public byte MajorVersion;
        /// <summary>
        /// 次要版本號。
        /// </summary>
        public byte MinorVersion;
        /// <summary>
        /// 記錄層內容長度。
        /// </summary>
        public ushort Length;
    }

    /// <summary>
    /// 取得或設定已解析之 TLS 記錄層標頭。
    /// </summary>
    public TlsProtocolHeader TlsHeader = new();
    /// <summary>
    /// 初始化 <see cref="AppLayerTlsSsl"/> 類別的新執行個體。
    /// </summary>
    /// <param name="byBuffer">緩衝區位元組陣列。</param>
    /// <param name="nReceived">接收到的位元組數量。</param>
    public AppLayerTlsSsl(byte[] byBuffer, int nReceived)
    {
        try
        {
            //Create MemoryStream out of the received bytes
            MemoryStream memoryStream = new(byBuffer, 0, nReceived);
            //Next we create a BinaryReader out of the MemoryStream
            BinaryReader binaryReader = new(memoryStream);
            TlsHeader.ContentType = binaryReader.ReadByte();
            TlsHeader.MajorVersion = binaryReader.ReadByte();
            TlsHeader.MinorVersion = binaryReader.ReadByte();
            TlsHeader.Length = binaryReader.ReadUInt16();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}

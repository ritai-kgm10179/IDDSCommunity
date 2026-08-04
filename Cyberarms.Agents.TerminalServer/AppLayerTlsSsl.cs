using System;
using System.IO;

namespace Cyberarms.Agents.TerminalServer;

public class AppLayerTlsSsl
{

    public const byte CONTENT_TYPE_SSL_APPLICATION_DATA = 0x17;
    public const byte CONTENT_TYPE_ENCRYPTED_ALERT = 0x15;
    public const byte CONTENT_TYPE_HANDSHAKE = 0x16;

    public struct TlsProtocolHeader
    {
        public byte ContentType;
        public byte MajorVersion;
        public byte MinorVersion;
        public ushort Length;
    }

    public TlsProtocolHeader TlsHeader = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLayerTlsSsl"/> class.
    /// </summary>
    /// <param name="byBuffer">The by buffer value.</param>
    /// <param name="nReceived">The n received value.</param>

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

using System;
using System.IO;
using System.Net;

namespace Cyberarms.Agents.MailServer;


public enum Protocol
{
    Tcp = 6,
    Udp = 17,
    Tlsp = 56,
    Unknown = -1
};

public class IPHeader
{
    //IP Header fields
    private readonly byte byVersionAndHeaderLength;   //Eight bits for version and header length
    private readonly byte byDifferentiatedServices;    //Eight bits for differentiated services (TOS)
    private readonly ushort usTotalLength;              //Sixteen bits for total length of the datagram (header + message)
    private readonly ushort usIdentification;           //Sixteen bits for identification
    private readonly ushort usFlagsAndOffset;           //Eight bits for flags and fragmentation offset
    private readonly byte byTTL;                      //Eight bits for TTL (Time To Live)
    private readonly byte byProtocol;                 //Eight bits for the underlying protocol
    private readonly short sChecksum;                  //Sixteen bits containing the checksum of the header
    //(checksum can be negative so taken as short)
    private readonly uint uiSourceIPAddress;          //Thirty two bit source IP Address
    private readonly uint uiDestinationIPAddress;     //Thirty two bit destination IP Address
    //End IP Header fields

    private readonly byte byHeaderLength;             //Header length
    private readonly byte[] byIPData = new byte[128];  //Data carried by the datagram


    public IPHeader(byte[] byBuffer, int nReceived)
    {

        try
        {
            //Create MemoryStream out of the received bytes
            MemoryStream memoryStream = new(byBuffer, 0, nReceived);
            //Next we create a BinaryReader out of the MemoryStream
            BinaryReader binaryReader = new(memoryStream);

            //The first eight bits of the IP header contain the version and
            //header length so we read them
            byVersionAndHeaderLength = binaryReader.ReadByte();

            //The next eight bits contain the Differentiated services
            byDifferentiatedServices = binaryReader.ReadByte();

            //Next eight bits hold the total length of the datagram
            usTotalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

            //Next sixteen have the identification bytes
            usIdentification = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

            //Next sixteen bits contain the flags and fragmentation offset
            usFlagsAndOffset = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

            //Next eight bits have the TTL value
            byTTL = binaryReader.ReadByte();

            //Next eight represnts the protocol encapsulated in the datagram
            byProtocol = binaryReader.ReadByte();

            //Next sixteen bits contain the checksum of the header
            sChecksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

            //Next thirty two bits have the source IP address
            uiSourceIPAddress = (uint)binaryReader.ReadInt32();

            //Next thirty two hold the destination IP address
            uiDestinationIPAddress = (uint)binaryReader.ReadInt32();

            //Now we calculate the header length

            byHeaderLength = byVersionAndHeaderLength;
            //The last four bits of the version and header length field contain the
            //header length, we perform some simple binary airthmatic operations to
            //extract them
            byHeaderLength <<= 4;
            byHeaderLength >>= 4;
            //Multiply by four to get the exact header length
            byHeaderLength *= 4;

            //Copy the data carried by the data gram into another array so that
            //according to the protocol being carried in the IP datagram
            Array.Copy(byBuffer,
                       byHeaderLength,  //start copying from the end of the header
                       byIPData, 0,
                       usTotalLength - byHeaderLength);
        }
        catch (Exception ex)
        {
            Sniffer.LogTrace(ex);
        }
    }

    public string Version
    {
        get
        {
            //Calculate the IP version

            //The four bits of the IP header contain the IP version
            if (byVersionAndHeaderLength >> 4 == 4)
            {
                return "IP v4";
            }
            else if (byVersionAndHeaderLength >> 4 == 6)
            {
                return "IP v6";
            }
            else
            {
                return "Unknown";
            }
        }
    }

    public string HeaderLength => byHeaderLength.ToString();

    public ushort MessageLength =>
        //MessageLength = Total length of the datagram - Header length
        (ushort)(usTotalLength - byHeaderLength);

    public string DifferentiatedServices
    {
        get
        {
            //Returns the differentiated services in hexadecimal format
            return string.Format("0x{0:x2} ({1})", byDifferentiatedServices,
                byDifferentiatedServices);
        }
    }

    public string Flags
    {
        get
        {
            //The first three bits of the flags and fragmentation field 
            //represent the flags (which indicate whether the data is 
            //fragmented or not)
            int nFlags = usFlagsAndOffset >> 13;
            if (nFlags == 2)
            {
                return "Don't fragment";
            }
            else if (nFlags == 1)
            {
                return "More fragments to come";
            }
            else
            {
                return nFlags.ToString();
            }
        }
    }

    public string FragmentationOffset
    {
        get
        {
            //The last thirteen bits of the flags and fragmentation field 
            //contain the fragmentation offset
            int nOffset = usFlagsAndOffset << 3;
            nOffset >>= 3;

            return nOffset.ToString();
        }
    }

    public string TTL => byTTL.ToString();

    public Protocol ProtocolType
    {
        get
        {
            //The protocol field represents the protocol in the data portion
            //of the datagram
            return byProtocol switch
            {
                6 => Protocol.Tcp,
                17 => Protocol.Udp,
                56 => Protocol.Tlsp,
                _ => Protocol.Unknown,
            };
        }
    }

    public string Checksum =>
        //Returns the checksum in hexadecimal format
        string.Format("0x{0:x2}", sChecksum);

    public IPAddress SourceAddress => new(uiSourceIPAddress);

    public IPAddress DestinationAddress => new(uiDestinationIPAddress);

    public string TotalLength => usTotalLength.ToString();

    public string Identification => usIdentification.ToString();

    public byte[] Data => byIPData;
}


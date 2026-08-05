using System;
using System.Text;
using System.IO;

namespace IDDSCommunity.Agents.MailServer;

public class AppLayerSmtp
{

    public const string SMTP_REPLY_CODE_LOGIN_DENIED = "504";

    public string SmtpReplyCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLayerSmtp"/> class.
    /// </summary>
    /// <param name="byBuffer">The by buffer value.</param>
    /// <param name="nReceived">The n received value.</param>

    public AppLayerSmtp(byte[] byBuffer, int nReceived)
    {
        try
        {
            //Create MemoryStream out of the received bytes
            MemoryStream memoryStream = new(byBuffer, 0, nReceived);
            //Next we create a BinaryReader out of the MemoryStream
            BinaryReader binaryReader = new(memoryStream);
            char[] replyCodeChars = binaryReader.ReadChars(3);
            StringBuilder replyCode = new();

            if (replyCodeChars.Length == 3)
            {
                for (int i = 0; i < 3; i++) replyCode.Append(replyCodeChars[i]);
            }
            SmtpReplyCode = replyCode.ToString();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}

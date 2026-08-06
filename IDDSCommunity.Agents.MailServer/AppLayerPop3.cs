using System;
using System.Text;
using System.IO;

namespace IDDSCommunity.Agents.MailServer;

public class AppLayerPop3
{
    public const string POP3_REPLY_CODE_ERROR = "-ERR";

    public const string POP3_INTERACTION_CODE_APOP = "APOP";
    public const string POP3_INTERACTION_CODE_DELE = "DELE";
    public const string POP3_INTERACTION_CODE_LIST = "LIST";
    public const string POP3_INTERACTION_CODE_NOOP = "NOOP";
    public const string POP3_INTERACTION_CODE_PASS = "PASS";
    public const string POP3_INTERACTION_CODE_QUIT = "QUIT";
    public const string POP3_INTERACTION_CODE_RETR = "RETR";
    public const string POP3_INTERACTION_CODE_RSET = "RSET";
    public const string POP3_INTERACTION_CODE_STAT = "STAT";
    public const string POP3_INTERACTION_CODE_TOP = "TOP ";
    public const string POP3_INTERACTION_CODE_UIDL = "UIDL";
    public const string POP3_INTERACTION_CODE_USER = "USER";


    public string Pop3Code { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="AppLayerPop3"/> 類別的新執行個體。
    /// </summary>
    /// <param name="byBuffer">緩衝區位元組陣列。</param>
    /// <param name="nReceived">接收到的位元組數量。</param>

    public AppLayerPop3(byte[] byBuffer, int nReceived)
    {
        try
        {
            if (nReceived > 3)
            {
                //Create MemoryStream out of the received bytes
                MemoryStream memoryStream = new(byBuffer, 0, nReceived);
                //Next we create a BinaryReader out of the MemoryStream
                BinaryReader binaryReader = new(memoryStream);
                char[] replyCodeChars = binaryReader.ReadChars(4);
                StringBuilder replyCode = new();

                if (replyCodeChars.Length == 4)
                {
                    for (int i = 0; i < 4; i++) replyCode.Append(replyCodeChars[i]);
                }
                Pop3Code = replyCode.ToString().ToUpper();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}

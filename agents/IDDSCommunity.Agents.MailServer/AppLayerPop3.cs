using System;
using System.Text;
using System.IO;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 解析單一 POP3 應用層封包，擷取回覆碼或互動命令碼前綴。
/// </summary>
public class AppLayerPop3
{
    /// <summary>
    /// POP3 錯誤回覆碼前綴。
    /// </summary>
    public const string POP3_REPLY_CODE_ERROR = "-ERR";

    /// <summary>
    /// POP3 APOP 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_APOP = "APOP";
    /// <summary>
    /// POP3 DELE 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_DELE = "DELE";
    /// <summary>
    /// POP3 LIST 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_LIST = "LIST";
    /// <summary>
    /// POP3 NOOP 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_NOOP = "NOOP";
    /// <summary>
    /// POP3 PASS 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_PASS = "PASS";
    /// <summary>
    /// POP3 QUIT 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_QUIT = "QUIT";
    /// <summary>
    /// POP3 RETR 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_RETR = "RETR";
    /// <summary>
    /// POP3 RSET 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_RSET = "RSET";
    /// <summary>
    /// POP3 STAT 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_STAT = "STAT";
    /// <summary>
    /// POP3 TOP 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_TOP = "TOP ";
    /// <summary>
    /// POP3 UIDL 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_UIDL = "UIDL";
    /// <summary>
    /// POP3 USER 命令前綴。
    /// </summary>
    public const string POP3_INTERACTION_CODE_USER = "USER";


    /// <summary>
    /// 取得或設定解析出的 POP3 回覆碼或命令碼前綴。
    /// </summary>
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

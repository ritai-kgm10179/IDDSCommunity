using System;
using System.Text;
using System.IO;

namespace IDDSCommunity.Agents.FtpServer;

/// <summary>
/// 代表解析後的 FTP 應用層通訊協定回應資料。
/// </summary>
public class AppLayerFtp
{
    /// <summary>
    /// 定義 FTP 登入遭拒之標準回應代碼 (530)。
    /// </summary>
    public const string FTP_REPLY_CODE_LOGIN_DENIED = "530";

    /// <summary>
    /// 取得或設定 FTP 回覆碼。
    /// </summary>
    public string FtpReplyCode { get; set; }
    /// <summary>
    /// 取得或設定完整的 FTP 回覆文字。
    /// </summary>
    public string ReplyText { get; set; }

    /// <summary>
    /// 取得回覆是否明確表示認證失敗。
    /// </summary>
    public bool IsAuthenticationFailure => FtpReplyCode == FTP_REPLY_CODE_LOGIN_DENIED &&
        (ReplyText.Contains("login incorrect", StringComparison.OrdinalIgnoreCase) ||
         ReplyText.Contains("cannot log in", StringComparison.OrdinalIgnoreCase) ||
         ReplyText.Contains("authentication failed", StringComparison.OrdinalIgnoreCase) ||
         ReplyText.Contains("password incorrect", StringComparison.OrdinalIgnoreCase));
    /// <summary>
    /// 初始化 <see cref="AppLayerFtp"/> 類別的新執行個體。
    /// </summary>
    /// <param name="byBuffer">緩衝區位元組陣列。</param>
    /// <param name="nReceived">接收到的位元組數量。</param>
    public AppLayerFtp(byte[] byBuffer, int nReceived)
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
            FtpReplyCode = replyCode.ToString();
            ReplyText = Encoding.ASCII.GetString(byBuffer, 0, nReceived);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}

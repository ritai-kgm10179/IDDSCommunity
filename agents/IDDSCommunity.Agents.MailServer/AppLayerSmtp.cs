using System;
using System.Text;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 解析單一 SMTP 應用層封包，擷取回覆碼。
/// </summary>
public class AppLayerSmtp
{

    /// <summary>
    /// SMTP 驗證被拒回覆碼。
    /// </summary>
    public const string SMTP_REPLY_CODE_LOGIN_DENIED = "535";

    /// <summary>
    /// 取得或設定解析出的 SMTP 回覆碼。
    /// </summary>
    public string SmtpReplyCode { get; set; }

    /// <summary>
    /// 以零記憶體配置快速檢查原始 ASCII 負載是否為 SMTP 535 驗證失敗回覆。
    /// </summary>
    /// <param name="payload">原始 TCP 應用層負載。</param>
    /// <returns>若前 3 個位元組為 <c>535</c> 則傳回 <see langword="true"/>。</returns>
    public static bool IsAuthenticationFailurePayload(ReadOnlySpan<byte> payload)
        => payload.Length >= 3 && payload[0] == (byte)'5' && payload[1] == (byte)'3' && payload[2] == (byte)'5';

    /// <summary>
    /// 初始化 <see cref="AppLayerSmtp"/> 類別的新執行個體。
    /// </summary>
    /// <param name="byBuffer">緩衝區位元組陣列。</param>
    /// <param name="nReceived">接收到的位元組數量。</param>
    public AppLayerSmtp(byte[] byBuffer, int nReceived)
        : this(byBuffer is null ? throw new ArgumentNullException(nameof(byBuffer)) : byBuffer.AsSpan(0, nReceived))
    {
    }

    /// <summary>
    /// 以唯讀位元組區段初始化 <see cref="AppLayerSmtp"/> 類別的新執行個體。
    /// </summary>
    /// <param name="payload">原始應用層位元組區段。</param>
    public AppLayerSmtp(ReadOnlySpan<byte> payload)
    {
        SmtpReplyCode = payload.Length >= 3 ? Encoding.ASCII.GetString(payload[..3]) : Encoding.ASCII.GetString(payload);
    }
}

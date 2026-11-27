using System;
using System.Text;

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
        IsAuthenticationFailureText(ReplyText.AsSpan());

    /// <summary>
    /// 以零記憶體配置快速檢查原始 ASCII 負載是否為 FTP 530 登入認證失敗回覆。
    /// </summary>
    /// <param name="payload">原始 TCP 應用層負載。</param>
    /// <returns>若符合 530 認證失敗特徵則傳回 <see langword="true"/>。</returns>
    public static bool IsAuthenticationFailurePayload(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 3 || payload[0] != (byte)'5' || payload[1] != (byte)'3' || payload[2] != (byte)'0')
            return false;

        Span<char> stackChars = payload.Length <= 512 ? stackalloc char[payload.Length] : new char[payload.Length];
        int chars = Encoding.ASCII.GetChars(payload, stackChars);
        return IsAuthenticationFailureText(stackChars[..chars]);
    }

    private static bool IsAuthenticationFailureText(ReadOnlySpan<char> text) =>
        text.Contains("login incorrect", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("cannot log in", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("authentication failed", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("password incorrect", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 初始化 <see cref="AppLayerFtp"/> 類別的新執行個體。
    /// </summary>
    /// <param name="byBuffer">緩衝區位元組陣列。</param>
    /// <param name="nReceived">接收到的位元組數量。</param>
    public AppLayerFtp(byte[] byBuffer, int nReceived)
        : this(byBuffer is null ? throw new ArgumentNullException(nameof(byBuffer)) : byBuffer.AsSpan(0, nReceived))
    {
    }

    /// <summary>
    /// 以唯讀位元組區段初始化 <see cref="AppLayerFtp"/> 類別的新執行個體。
    /// </summary>
    /// <param name="payload">原始應用層位元組區段。</param>
    public AppLayerFtp(ReadOnlySpan<byte> payload)
    {
        FtpReplyCode = payload.Length >= 3 ? Encoding.ASCII.GetString(payload[..3]) : Encoding.ASCII.GetString(payload);
        ReplyText = Encoding.ASCII.GetString(payload);
    }
}

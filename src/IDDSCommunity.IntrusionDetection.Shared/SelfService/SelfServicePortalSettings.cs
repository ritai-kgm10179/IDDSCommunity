namespace IDDSCommunity.IntrusionDetection.Shared.SelfService;

/// <summary>
/// 定義自助解鎖門戶之驗證方式。
/// </summary>
public enum SelfServiceVerificationMethod
{
    /// <summary>
    /// RFC 6238 TOTP 雙因子動態驗證碼 (Google/Microsoft Authenticator)。
    /// </summary>
    TotpAuthenticator = 0,

    /// <summary>
    /// Cloudflare Turnstile 隱私無感真人驗證。
    /// </summary>
    CloudflareTurnstile = 1,

    /// <summary>
    /// Google reCAPTCHA v2 / v3 驗證。
    /// </summary>
    GoogleReCaptcha = 2
}

/// <summary>
/// 自助驗證解鎖門戶之組態設定模型。
/// </summary>
public sealed class SelfServicePortalSettings
{
    /// <summary>
    /// 取得或設定是否啟用合法使用者自助驗證解鎖門戶。
    /// </summary>
    public bool EnableSelfServicePortal { get; set; } = false;

    /// <summary>
    /// 取得或設定門戶 HTTP 監聽連接埠 (預設 8444)。
    /// </summary>
    public int PortalPort { get; set; } = 8444;

    /// <summary>
    /// 取得或設定門戶監聽 IP 位址 (預設 "0.0.0.0")。
    /// </summary>
    public string PortalListenIp { get; set; } = "0.0.0.0";

    /// <summary>
    /// 取得或設定預設驗證方式。
    /// </summary>
    public SelfServiceVerificationMethod VerificationMethod { get; set; } = SelfServiceVerificationMethod.TotpAuthenticator;

    /// <summary>
    /// 取得或設定 RFC 6238 TOTP 共享 Base32 密鑰。
    /// </summary>
    public string TotpBase32Secret { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Cloudflare Turnstile / reCAPTCHA Site Key。
    /// </summary>
    public string CaptchaSiteKey { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Cloudflare Turnstile / reCAPTCHA Secret Key。
    /// </summary>
    public string CaptchaSecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定單一 IP 最大連續驗證失敗次數 (超過立即升級硬封鎖，預設 3)。
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 3;
}

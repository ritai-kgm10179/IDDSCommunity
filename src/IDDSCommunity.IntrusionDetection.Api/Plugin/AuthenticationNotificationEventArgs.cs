namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 表示可跨擴充元件邊界完整傳遞之驗證安全事件通知。
/// </summary>
public sealed class AuthenticationNotificationEventArgs : NotificationEventArgs
{
    /// <summary>
    /// 取得或設定正規化前的目標帳號名稱。
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定事件是否為明確的密碼或帳號憑證驗證失敗。
    /// </summary>
    public bool IsCredentialFailure { get; set; } = true;

    /// <summary>
    /// 取得或設定事件提供者或通道名稱。
    /// </summary>
    public string ProviderOrChannel { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定產生事件的電腦名稱。
    /// </summary>
    public string ComputerName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Windows 事件記錄識別碼。
    /// </summary>
    public long? SourceEventRecordId { get; set; }

    /// <summary>
    /// 取得或設定 Windows 活動或關聯識別碼。
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// 取得或設定事件可信度分數。
    /// </summary>
    public double ConfidenceScore { get; set; } = 1.0;

    /// <summary>
    /// 取得或設定事件所涉及的目標資源。
    /// </summary>
    public string? TargetResource { get; set; }

    /// <summary>
    /// 取得或設定原始錯誤碼。
    /// </summary>
    public string? ErrorCode { get; set; }
}

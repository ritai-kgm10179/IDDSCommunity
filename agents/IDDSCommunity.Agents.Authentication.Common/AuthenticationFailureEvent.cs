using System;
using System.Net;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 表示由事件來源解析出的單筆驗證失敗事件。
/// </summary>
/// <param name="OccurredAt">事件實際發生時間。</param>
/// <param name="SourceAddress">觸發失敗事件的來源 IP 位址。</param>
/// <param name="EventId">原始事件識別碼。</param>
/// <param name="Category">事件所屬類別（例如通訊協定或服務名稱）。</param>
/// <param name="AccountName">失敗事件關聯之帳戶名稱；無法取得時為空字串。</param>
/// <param name="Reason">失敗原因描述。</param>
/// <param name="IsCredentialFailure">指出此事件是否為明確之認證憑證失敗（密碼錯誤/帳號不存在）。</param>
/// <param name="ActivityId">關聯之 Windows 活動識別碼 (ActivityId/CorrelationId)。</param>
/// <param name="ConfidenceScore">事件置信度分數 (0.0 至 1.0)。</param>
/// <param name="ProviderOrChannel">事件提供者或通道名稱。</param>
/// <param name="ComputerName">產生事件的電腦名稱。</param>
/// <param name="SourceEventRecordId">Windows 事件記錄識別碼。</param>
/// <param name="TargetResource">事件所涉及的目標資源。</param>
/// <param name="ErrorCode">原始錯誤碼。</param>
public sealed record AuthenticationFailureEvent(
    DateTimeOffset OccurredAt,
    IPAddress SourceAddress,
    int EventId,
    string Category,
    string AccountName,
    string Reason,
    bool IsCredentialFailure = true,
    string? ActivityId = null,
    double ConfidenceScore = 1.0,
    string ProviderOrChannel = "",
    string ComputerName = "",
    long? SourceEventRecordId = null,
    string? TargetResource = null,
    string? ErrorCode = null);

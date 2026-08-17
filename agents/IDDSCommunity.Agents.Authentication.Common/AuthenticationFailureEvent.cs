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
public sealed record AuthenticationFailureEvent(
    DateTimeOffset OccurredAt,
    IPAddress SourceAddress,
    int EventId,
    string Category,
    string AccountName,
    string Reason);

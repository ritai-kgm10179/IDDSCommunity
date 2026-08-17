using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Sockets;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WebSecurity;

/// <summary>
/// 監看 Windows 應用程式事件記錄中內嵌用戶端 IP 之存取被拒事件，並透過共用驗證失敗偵測框架
/// （滑動時間窗、事件去重、門檻值判斷）決定是否達到攻擊門檻，避免單次事件即觸發告警。
/// </summary>
[Plugin("Web Security Agent", "Detects repeated Windows Application log access-denied events carrying an embedded client IP.", "1.0")]
public sealed class WebSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    private const string SEARCH_PATTERN_BEGIN = "[IP = '";
    private const string SEARCH_PATTERN_END = "']";

    internal const string EVENT_LOG_QUERY_IDDSCOMMUNITY_IIS_SECURITY_MONITOR_ACCESS_DENIED = @"<QueryList>
                  <Query Id=""4625"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=4625) and
                        TimeCreated[timediff(@SystemTime) &lt;= 864000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// 初始化 <see cref="WebSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public WebSecurityAgent() : base(new WindowsEventLogFailureSource("Application", EVENT_LOG_QUERY_IDDSCOMMUNITY_IIS_SECURITY_MONITOR_ACCESS_DENIED, Parse)) { }

    /// <summary>
    /// 以自訂事件來源初始化 <see cref="WebSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal WebSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName
    {
        get => IntrusionDetection.Api.Localization.Strings.Get("Web Security Agent");
        set { }
    }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{63F5567C-7A75-4870-A842-E981855DA3E9}");

    /// <summary>
    /// 從事件記錄的屬性中擷取形如 <c>[IP = 'x.x.x.x']</c> 的用戶端位址，並解析為驗證失敗事件；
    /// 未找到有效 IPv4／IPv6 位址時傳回 <see langword="null"/>。
    /// </summary>
    /// <param name="record">待解析之事件記錄。</param>
    /// <returns>解析成功時傳回驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        foreach (EventProperty property in record.Properties)
        {
            string? propertyValue = property.Value?.ToString();
            if (propertyValue?.Contains(SEARCH_PATTERN_BEGIN, StringComparison.Ordinal) != true)
                continue;

            int start = propertyValue.IndexOf(SEARCH_PATTERN_BEGIN, StringComparison.Ordinal) + SEARCH_PATTERN_BEGIN.Length;
            int end = propertyValue.IndexOf(SEARCH_PATTERN_END, start, StringComparison.Ordinal);
            if (end <= start)
                continue;

            string ipAddress = propertyValue[start..end];
            if (!IPAddress.TryParse(ipAddress, out IPAddress? probe))
                continue;
            if (probe.AddressFamily != AddressFamily.InterNetwork && probe.AddressFamily != AddressFamily.InterNetworkV6)
                continue;

            DateTimeOffset occurredAt = record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow;
            return new AuthenticationFailureEvent(occurredAt, probe, record.Id, "Web Security", string.Empty, "Access denied");
        }
        return null;
    }
}

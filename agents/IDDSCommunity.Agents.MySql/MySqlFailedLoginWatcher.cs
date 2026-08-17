using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MySql;

/// <summary>
/// 監看 Windows 應用程式事件記錄中的 MySQL／MariaDB 驗證失敗事件，並透過共用驗證失敗偵測框架
/// （滑動時間窗、事件去重、門檻值判斷）決定是否達到攻擊門檻，避免單次登入失敗即觸發告警。
/// </summary>
[Plugin("MySQL and MariaDB Security Agent", "Detects repeated MySQL/MariaDB authentication failures.", "1.0")]
public sealed class MySqlFailedLoginWatcher : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    internal const string EventLogQuery = @"<QueryList>
                  <Query Id=""0"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[Provider[@Name='MySQL' or @Name='MariaDB']]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// 初始化 <see cref="MySqlFailedLoginWatcher"/> 類別的新執行個體。
    /// </summary>
    public MySqlFailedLoginWatcher() : base(new WindowsEventLogFailureSource("Application", EventLogQuery, Parse)) { }

    /// <summary>
    /// 以自訂事件來源初始化 <see cref="MySqlFailedLoginWatcher"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal MySqlFailedLoginWatcher(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName
    {
        get => IntrusionDetection.Api.Localization.Strings.Get("MySQL and MariaDB Security Agent");
        set { }
    }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{EE4906AD-7242-4940-A3B0-81B4E3F16B71}");

    /// <summary>
    /// 將單筆 Windows 事件記錄解析為驗證失敗事件；若非可辨識的存取被拒事件則傳回 <see langword="null"/>。
    /// </summary>
    /// <param name="record">待解析之事件記錄。</param>
    /// <returns>解析成功時傳回驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        List<string?> messages = [];
        foreach (EventProperty property in record.Properties) messages.Add(property.Value?.ToString());
        try { messages.Add(record.FormatDescription()); }
        catch (EventLogException exception) { Trace.TraceWarning("Unable to format MySQL/MariaDB event {0}: {1}", record.Id, exception.Message); }

        if (!MySqlMariaDbAuthenticationParser.TryParse(record.ProviderName, messages, out IPAddress address)) return null;
        DateTimeOffset occurredAt = record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow;
        return new AuthenticationFailureEvent(occurredAt, address, record.Id, "MySQL/MariaDB", string.Empty, "Access denied");
    }
}

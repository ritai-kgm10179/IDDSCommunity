using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;
/// <summary>
/// 掃描與監控系統事件紀錄中 Active Directory 憑證驗證失敗攻擊之入侵偵測 Agent。
/// </summary>
[Plugin("Active Directory Credential validation Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public class AdCredentialValidationSecurityAgent : AgentPlugin, IExtendedInformation
{
    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""0"" Path=""Security"">
                    <Select Path=""Security"">
                        *[System[(EventID=4776) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]] and
                        *[EventData[Data[@Name='Status']='0xC0000064' or
                        Data[@Name='Status']='0xC000006A' or
                        Data[@Name='Status']='0xC000006D']]
                    </Select>
                  </Query>
                </QueryList>";
    /// <summary>
    /// 初始化 <see cref="AdCredentialValidationSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public AdCredentialValidationSecurityAgent()
    {
    }
    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Security", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
        watcher.Enabled = true;
    }
    /// <summary>
    /// 從暫停狀態復原 Agent 服務。
    /// </summary>
    protected override void OnContinueAgent() => watcher!.Enabled = true;
    /// <summary>
    /// 暫停 Agent 服務。
    /// </summary>
    protected override void OnPauseAgent() => watcher!.Enabled = false;
    /// <summary>
    /// 停止 Agent 服務並釋放事件紀錄監聽器。
    /// </summary>
    protected override void OnStopAgent()
    {
        watcher?.Dispose();
        watcher = null;
        query = null;
    }
    private static readonly EventLogPropertySelector AdPropertySelector = new(
    [
        @"Event/EventData/Data[@Name=""Workstation""]",
        @"Event/EventData/Data[@Name=""TargetUserName""]",
        @"Event/EventData/Data[@Name=""Status""]"
    ]);
    private static readonly ConcurrentDictionary<string, (string[] Ips, long ExpiresAtTicks)> DnsCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DnsCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DnsLookupTimeout = TimeSpan.FromSeconds(2);
    private const int MaxDnsCacheEntries = 1024;

    /// <summary>
    /// 處理事件紀錄寫入事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件紀錄寫入參數。</param>
    private void watcher_EventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            using EventRecord? eventRecord = e.EventRecord;
            if (eventRecord is not EventLogRecord record)
                return;
            IList<object?> values = record.GetPropertyValues(AdPropertySelector);
            string hostName = values[0]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(hostName) ||
                string.Equals(hostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(hostName, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            string[] ipAddresses = ResolveIp(hostName);
            foreach (string ipAddress in ipAddresses)
            {
                AuthenticationNotificationEventArgs args = new()
                {
                    CreateDate = record.TimeCreated ?? DateTime.Now,
                    EventId = record.Id,
                    IpAddress = ipAddress,
                    AccountName = values[1]?.ToString() ?? string.Empty,
                    IsCredentialFailure = true,
                    ProviderOrChannel = record.LogName ?? "Security",
                    ComputerName = record.MachineName ?? string.Empty,
                    SourceEventRecordId = record.RecordId,
                    ActivityId = record.ActivityId?.ToString("D"),
                    ErrorCode = values[2]?.ToString()
                };
                OnAttackDetected(this, args);
            }
        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase.AdCredentialValidation", ex.Message);
        }
    }
    /// <summary>
    /// 解析主機名稱對應的 IP 位址。
    /// </summary>
    /// <param name="hostname">主機名稱。</param>
    /// <returns>傳回包含解析出的 IP 位址字串陣列。</returns>
    private static string[] ResolveIp(string hostname)
    {
        if (IPAddress.TryParse(hostname, out IPAddress? parsed))
        {
            return (parsed.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6) && !IPAddress.IsLoopback(parsed)
                ? [parsed.ToString()]
                : [];
        }

        long nowTicks = Environment.TickCount64;
        if (DnsCache.TryGetValue(hostname, out (string[] Ips, long ExpiresAtTicks) cached) && cached.ExpiresAtTicks > nowTicks)
        {
            return cached.Ips;
        }

        string[] resolved = [];
        try
        {
            using CancellationTokenSource cts = new(DnsLookupTimeout);
            IPAddress[] addr = Dns.GetHostAddressesAsync(hostname, cts.Token).GetAwaiter().GetResult();
            if (addr.Length > 0)
            {
                List<string> list = new(addr.Length);
                foreach (IPAddress ip in addr)
                {
                    if (ip.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                    {
                        list.Add(ip.ToString());
                    }
                }
                resolved = list.ToArray();
            }
        }
        catch
        {
            // DNS 解析失敗或逾時時短暫快取空結果，避免重複阻塞事件監聽執行緒。
        }

        if (DnsCache.Count >= MaxDnsCacheEntries)
        {
            foreach (KeyValuePair<string, (string[] Ips, long ExpiresAtTicks)> entry in DnsCache)
            {
                if (entry.Value.ExpiresAtTicks <= nowTicks || DnsCache.Count >= MaxDnsCacheEntries)
                {
                    DnsCache.TryRemove(entry.Key, out _);
                }
                if (DnsCache.Count < MaxDnsCacheEntries / 2)
                {
                    break;
                }
            }
        }

        long ttlMs = resolved.Length > 0 ? (long)DnsCacheTtl.TotalMilliseconds : 60_000;
        DnsCache[hostname] = (resolved, nowTicks + ttlMs);
        return resolved;
    }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => Api.Localization.Strings.Get("AD Credential Validation Security Agent"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
    }
    /// <summary>
    /// 取得或設定 Agent 的預設圖示。
    /// </summary>
    public Image? Icon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? SelectedIcon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? UnselectedIcon { get; set; }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public Guid Id => new("{D67852B4-DBEF-4831-877C-E37DAB764952}");
}

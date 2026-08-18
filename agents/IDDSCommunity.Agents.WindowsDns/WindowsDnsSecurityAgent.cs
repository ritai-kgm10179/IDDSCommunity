using System;
using System.Diagnostics;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsDns;

/// <summary>
/// 監控 Windows DNS Server 分析與稽核事件以偵測異常客戶端查詢之安全代理程式。
/// </summary>
[Plugin("Windows DNS Security Agent", "Detects abusive Windows DNS Server clients from official analytical and audit events.", "1.0")]
public sealed class WindowsDnsSecurityAgent : AgentPlugin, IExtendedInformation
{
    private readonly IWindowsDnsEventSource eventSource;
    private readonly TimeProvider timeProvider;
    private DnsThreatDetector? detector;
    /// <summary>
    /// 初始化包含 Windows 事件紀錄訂閱的正式 Agent。
    /// </summary>
    public WindowsDnsSecurityAgent() : this(new WindowsDnsEventLogSource(), TimeProvider.System)
    {
    }

    internal WindowsDnsSecurityAgent(IWindowsDnsEventSource eventSource, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(eventSource);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.eventSource = eventSource;
        this.timeProvider = timeProvider;
        Configuration = new AgentConfigurationBase
        {
            AgentName = nameof(WindowsDnsSecurityAgent),
            ConfigurationSettingsTypeName = typeof(WindowsDnsConfiguration).FullName ?? string.Empty,
            AgentSettings = new WindowsDnsConfiguration()
        };
    }

    /// <summary>
    /// 驗證目前設定、建立威脅偵測器，並啟動事件來源。
    /// </summary>
    /// <exception cref="InvalidOperationException">設定物件型別不符或設定驗證失敗。</exception>
    protected override void OnStartAgent()
    {
        WindowsDnsConfiguration configuration = GetConfiguration();
        configuration.Validate();
        detector = new DnsThreatDetector(configuration, timeProvider);
        eventSource.EventReceived += OnEventReceived;
        eventSource.Error += OnEventSourceError;
        try
        {
            eventSource.Start();
        }
        catch
        {
            eventSource.EventReceived -= OnEventReceived;
            eventSource.Error -= OnEventSourceError;
            detector = null;
            throw;
        }
    }

    /// <summary>
    /// 暫停底層事件來源的事件接收。
    /// </summary>
    protected override void OnPauseAgent() => eventSource.Pause();
    /// <summary>
    /// 從暫停狀態恢復底層事件來源的事件接收。
    /// </summary>
    protected override void OnContinueAgent() => eventSource.Resume();

    /// <summary>
    /// 停止底層事件來源，同時清除威脅偵測器狀態。
    /// </summary>
    protected override void OnStopAgent()
    {
        eventSource.EventReceived -= OnEventReceived;
        eventSource.Error -= OnEventSourceError;
        eventSource.Stop();
        detector = null;
    }

    private void OnEventReceived(object? sender, DnsEventRecord record)
    {
        WindowsDnsMetrics.RecordObserved();
        DnsDetection? detection = detector?.Analyze(record);
        if (detection is null)
            return;
        WindowsDnsMetrics.RecordDetected();
        NotificationEventArgs args = new()
        {
            CreateDate = detection.SourceEvent.OccurredAt.LocalDateTime,
            EventId = detection.SourceEvent.EventId,
            IpAddress = detection.SourceEvent.SourceAddress.ToString(),
            EventMessage = GetDetectionMessage(detection)
        };
        OnAttackDetected(this, args);
    }

    private static void OnEventSourceError(Exception exception) => Trace.TraceError("{0}: {1}", DnsStrings.Get("Windows DNS event subscription failed."), exception.Message);

    private static string GetDetectionMessage(DnsDetection detection) => detection.Type switch
    {
        DnsDetectionType.QueryRate => DnsStrings.Format("DNS query-rate threshold exceeded by {0}.", detection.SourceEvent.SourceAddress),
        DnsDetectionType.NxDomainRate => DnsStrings.Format("DNS NXDOMAIN threshold exceeded by {0}.", detection.SourceEvent.SourceAddress),
        DnsDetectionType.AnyQueryRate => DnsStrings.Format("DNS ANY-query threshold exceeded by {0}.", detection.SourceEvent.SourceAddress),
        DnsDetectionType.DynamicUpdateRate => DnsStrings.Format("DNS dynamic-update threshold exceeded by {0}.", detection.SourceEvent.SourceAddress),
        DnsDetectionType.ZoneTransfer => DnsStrings.Format("DNS zone-transfer threshold exceeded by {0}.", detection.SourceEvent.SourceAddress),
        _ => DnsStrings.Format("Suspicious DNS activity was detected from {0}.", detection.SourceEvent.SourceAddress)
    };

    private WindowsDnsConfiguration GetConfiguration() =>
        Configuration.AgentSettings as WindowsDnsConfiguration ?? throw new InvalidOperationException(DnsStrings.Get("Windows DNS Agent configuration is unavailable."));
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => DnsStrings.Get("Windows DNS Security Agent");
        set { }
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
    public Guid Id => new("{0E5C35B5-7B2E-4DD5-970D-89A33C935A51}");
}

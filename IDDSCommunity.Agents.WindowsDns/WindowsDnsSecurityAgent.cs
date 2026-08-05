using System;
using System.Diagnostics;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsDns;

[Plugin("Windows DNS Security Agent", "Detects abusive Windows DNS Server clients from official analytical and audit events.", "1.0")]
public sealed class WindowsDnsSecurityAgent : AgentPlugin, IExtendedInformation
{
    private readonly IWindowsDnsEventSource eventSource;
    private readonly TimeProvider timeProvider;
    private DnsThreatDetector? detector;

    /// <summary>
    /// Initializes the production Agent with Windows Event Log subscriptions.
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

    protected override void OnPauseAgent() => eventSource.Pause();
    protected override void OnContinueAgent() => eventSource.Resume();

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

    public string DisplayName
    {
        get => DnsStrings.Get("Windows DNS Security Agent");
        set { }
    }

    public Image Icon
    {
        get => Resource.agent15px_dns_dark;
        set { }
    }

    public Image SelectedIcon
    {
        get => Resource.agent15px_dns_white;
        set { }
    }

    public Image UnselectedIcon
    {
        get => Resource.agent15px_dns_dark;
        set { }
    }

    public Guid Id => new("{0E5C35B5-7B2E-4DD5-970D-89A33C935A51}");
}

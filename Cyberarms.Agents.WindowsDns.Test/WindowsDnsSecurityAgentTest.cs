using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Cyberarms.IntrusionDetection.Api.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.Agents.WindowsDns.Test;

[TestClass]
[DoNotParallelize]
public sealed class WindowsDnsSecurityAgentTest
{
    /// <summary>
    /// Restores the process UI culture after localization assertions.
    /// </summary>
    [TestCleanup]
    public void Cleanup() => CultureInfo.CurrentUICulture = new CultureInfo("en-US");

    /// <summary>
    /// Verifies the documented payload positions for responses, updates, transfers, and audit events.
    /// </summary>
    [TestMethod]
    public void TryParse_DocumentedEvents_ExtractsSourceAndDnsFields()
    {
        object?[] response = [false, "10.0.0.53", "192.0.2.10", false, false, "missing.example", "ANY", 1, false, "NXDOMAIN"];
        object?[] update = [false, "10.0.0.53", "192.0.2.20", "host.example"];
        object?[] axfr = [true, "192.0.2.30", "10.0.0.53", "example.com"];
        object?[] auditUpdate = ["A", "host.example", 300, string.Empty, "192.0.2.40", "example.com", "default", "192.0.2.41"];

        Assert.IsTrue(WindowsDnsEventParser.TryParse(257, response, DateTimeOffset.UtcNow, out DnsEventRecord? responseRecord));
        Assert.IsTrue(WindowsDnsEventParser.TryParse(263, update, DateTimeOffset.UtcNow, out DnsEventRecord? updateRecord));
        Assert.IsTrue(WindowsDnsEventParser.TryParse(270, axfr, DateTimeOffset.UtcNow, out DnsEventRecord? transferRecord));
        Assert.IsTrue(WindowsDnsEventParser.TryParse(519, auditUpdate, DateTimeOffset.UtcNow, out DnsEventRecord? auditRecord));

        Assert.AreEqual(IPAddress.Parse("192.0.2.10"), responseRecord!.SourceAddress);
        Assert.IsTrue(responseRecord.IsNxDomain);
        Assert.IsTrue(responseRecord.IsAnyQuery);
        Assert.AreEqual(DnsActivityKind.DynamicUpdate, updateRecord!.Kind);
        Assert.AreEqual(DnsActivityKind.ZoneTransfer, transferRecord!.Kind);
        Assert.AreEqual(IPAddress.Parse("192.0.2.41"), auditRecord!.SourceAddress);
        Assert.AreEqual(DnsActivityKind.DynamicUpdate, auditRecord.Kind);
    }

    /// <summary>
    /// Verifies unsupported events and invalid source addresses are rejected.
    /// </summary>
    [TestMethod]
    public void TryParse_UnsupportedOrInvalidEvent_ReturnsFalse()
    {
        Assert.IsFalse(WindowsDnsEventParser.TryParse(999, [], DateTimeOffset.UtcNow, out _));
        Assert.IsFalse(WindowsDnsEventParser.TryParse(257, [false, "10.0.0.53", "not-an-ip"], DateTimeOffset.UtcNow, out _));
    }

    /// <summary>
    /// Verifies each suspicious DNS category emits once when its configured boundary is crossed.
    /// </summary>
    [TestMethod]
    public void Analyze_ThresholdCrossings_ReturnExpectedDetections()
    {
        ManualTimeProvider time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        WindowsDnsConfiguration configuration = CreateConfiguration();
        DnsThreatDetector detector = new(configuration, time);
        IPAddress source = IPAddress.Parse("192.0.2.10");

        Assert.IsNull(detector.Analyze(CreateQuery(source)));
        Assert.AreEqual(DnsDetectionType.QueryRate, detector.Analyze(CreateQuery(source))!.Type);

        IPAddress nxSource = IPAddress.Parse("192.0.2.11");
        Assert.IsNull(detector.Analyze(CreateQuery(nxSource, responseCode: "3")));
        Assert.AreEqual(DnsDetectionType.NxDomainRate, detector.Analyze(CreateQuery(nxSource, responseCode: "NXDOMAIN"))!.Type);

        IPAddress anySource = IPAddress.Parse("192.0.2.12");
        Assert.IsNull(detector.Analyze(CreateQuery(anySource, queryType: "255")));
        Assert.AreEqual(DnsDetectionType.AnyQueryRate, detector.Analyze(CreateQuery(anySource, queryType: "ANY"))!.Type);

        Assert.AreEqual(DnsDetectionType.ZoneTransfer, detector.Analyze(new DnsEventRecord(270, time.UtcNow, IPAddress.Parse("192.0.2.13"), DnsActivityKind.ZoneTransfer, "example.com", string.Empty, string.Empty))!.Type);

        IPAddress updateSource = IPAddress.Parse("192.0.2.14");
        Assert.IsNull(detector.Analyze(new DnsEventRecord(263, time.UtcNow, updateSource, DnsActivityKind.DynamicUpdate, "host.example", string.Empty, string.Empty)));
        Assert.AreEqual(DnsDetectionType.DynamicUpdateRate, detector.Analyze(new DnsEventRecord(263, time.UtcNow, updateSource, DnsActivityKind.DynamicUpdate, "host.example", string.Empty, string.Empty))!.Type);
    }

    /// <summary>
    /// Verifies excluded addresses, window resets, and client tracking capacity.
    /// </summary>
    [TestMethod]
    public void Analyze_ExclusionsWindowsAndCapacity_RemainBounded()
    {
        ManualTimeProvider time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        WindowsDnsConfiguration configuration = CreateConfiguration();
        configuration.MaximumTrackedClients = 100;
        DnsThreatDetector detector = new(configuration, time);

        Assert.IsNull(detector.Analyze(CreateQuery(IPAddress.Loopback)));
        for (int index = 1; index <= 120; index++)
            detector.Analyze(CreateQuery(IPAddress.Parse($"198.51.100.{(index % 254) + 1}")));
        Assert.IsLessThanOrEqualTo(100, detector.TrackedClientCount);

        IPAddress source = IPAddress.Parse("203.0.113.10");
        detector.Analyze(CreateQuery(source));
        time.UtcNow = time.UtcNow.AddSeconds(configuration.WindowSeconds);
        Assert.IsNull(detector.Analyze(CreateQuery(source)));
    }

    /// <summary>
    /// Verifies Agent lifecycle wiring and conversion of one detector signal to the existing attack contract.
    /// </summary>
    [TestMethod]
    public void Agent_LifecycleAndDetection_UseExistingAttackContract()
    {
        FakeEventSource source = new();
        ManualTimeProvider time = new(DateTimeOffset.UtcNow);
        WindowsDnsSecurityAgent agent = new(source, time);
        WindowsDnsConfiguration configuration = (WindowsDnsConfiguration)agent.Configuration.AgentSettings!;
        configuration.QueryRateThreshold = 2;
        INotificationEventArgs? notification = null;
        agent.AttackDetected += (_, args) => notification = args;

        agent.Start();
        source.Raise(CreateQuery(IPAddress.Parse("192.0.2.80")));
        source.Raise(CreateQuery(IPAddress.Parse("192.0.2.80")));
        agent.Pause();
        agent.Continue();
        agent.Stop();

        Assert.AreEqual("192.0.2.80", notification!.IpAddress);
        Assert.AreEqual(1, source.StartCount);
        Assert.AreEqual(1, source.PauseCount);
        Assert.AreEqual(1, source.ResumeCount);
        Assert.AreEqual(1, source.StopCount);
    }

    /// <summary>
    /// Verifies source errors do not terminate the Agent event pipeline.
    /// </summary>
    [TestMethod]
    public void Agent_EventSourceError_DoesNotStopSubscription()
    {
        FakeEventSource source = new();
        WindowsDnsSecurityAgent agent = new(source, TimeProvider.System);

        agent.Start();
        source.RaiseError(new InvalidOperationException("expected test failure"));
        source.Raise(CreateQuery(IPAddress.Parse("192.0.2.90")));
        agent.Stop();

        Assert.AreEqual(1, source.StartCount);
        Assert.AreEqual(1, source.StopCount);
    }

    /// <summary>
    /// Verifies the Agent display name and detection messages are localized.
    /// </summary>
    [TestMethod]
    public void DisplayName_TraditionalChinese_IsLocalized()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("zh-TW");
        using FakeEventSource source = new();
        WindowsDnsSecurityAgent agent = new(source, TimeProvider.System);

        Assert.AreEqual("Windows DNS 安全防護 Agent", agent.DisplayName);
    }

    private static WindowsDnsConfiguration CreateConfiguration() => new()
    {
        WindowSeconds = 60,
        QueryRateThreshold = 2,
        NxDomainThreshold = 2,
        AnyQueryThreshold = 2,
        DynamicUpdateThreshold = 2,
        ZoneTransferThreshold = 1,
        MaximumTrackedClients = 100,
        ExcludedAddresses = "127.0.0.1;::1"
    };

    private static DnsEventRecord CreateQuery(IPAddress source, string queryType = "A", string responseCode = "0") =>
        new(257, DateTimeOffset.UtcNow, source, DnsActivityKind.Query, "example.com", queryType, responseCode);

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class FakeEventSource : IWindowsDnsEventSource
    {
        public event EventHandler<DnsEventRecord>? EventReceived;
        public event Action<Exception>? Error;
        internal int StartCount { get; private set; }
        internal int PauseCount { get; private set; }
        internal int ResumeCount { get; private set; }
        internal int StopCount { get; private set; }
        public void Start() => StartCount++;
        public void Pause() => PauseCount++;
        public void Resume() => ResumeCount++;
        public void Stop() => StopCount++;
        public void Dispose() => Stop();
        internal void Raise(DnsEventRecord record) => EventReceived?.Invoke(this, record);
        internal void RaiseError(Exception exception) => Error?.Invoke(exception);
    }
}

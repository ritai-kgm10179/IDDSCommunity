namespace IDDSCommunity.Agents.RemoteDesktopGateway.Test;

using System;
using System.Collections.Generic;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.RemoteDesktopGateway;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 驗證 Remote Desktop Gateway (RD Gateway) 與關聯來源之事件剖析、NPS 語意與安全代理完整功能矩陣。
/// </summary>
[TestClass]
public class RdGatewayEventParserTest
{
    /// <summary>
    /// 驗證具 Windows Event XML 命名空間的固定 NPS 事件可正確擷取具名欄位。
    /// </summary>
    [TestMethod]
    public void ReadXml_NamespacedNpsFixture_ExtractsNamedFields()
    {
        const string xml = """
            <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
              <EventData>
                <Data Name="AccountName">CORP\fixture-user</Data>
                <Data Name="CallingStationID">203.0.113.44</Data>
                <Data Name="ReasonCode">16</Data>
              </EventData>
            </Event>
            """;

        IReadOnlyDictionary<string, string> fields = RdGatewayEventParser.ReadNamedAndPositionalFields(xml);
        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 6273, "Security");

        Assert.IsNotNull(failure);
        Assert.AreEqual(@"CORP\fixture-user", failure.AccountName);
        Assert.IsTrue(failure.IsCredentialFailure);
    }

    /// <summary>
    /// 驗證已知事件識別碼出現在未知通道時安全降級，不會被誤歸類為 RD Gateway 或 NPS 事件。
    /// </summary>
    [TestMethod]
    public void Parse_UnknownChannelWithKnownEventIds_ReturnsNull()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username"] = @"CORP\fixture-user",
            ["IpAddress"] = "203.0.113.44",
            ["ReasonCode"] = "16"
        };

        Assert.IsNull(RdGatewayEventParser.TryParseFields(fields, DateTimeOffset.UtcNow, 201, "Unknown/Operational"));
        Assert.IsNull(RdGatewayEventParser.TryParseFields(fields, DateTimeOffset.UtcNow, 6273, "Unknown/Security"));
    }

    /// <summary>
    /// 驗證 TerminalServices-Gateway Event 201 (CAP 拒絕) 標記為非密碼驗證失敗 (IsCredentialFailure=false)。
    /// </summary>
    [TestMethod]
    public void Parse_Gateway_Event201_CapDenied_IsNonCredentialFailure()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username"] = @"CORP\Bob",
            ["IpAddress"] = "192.0.2.140",
            ["ErrorCode"] = "23003"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, now, 201, "Microsoft-Windows-TerminalServices-Gateway/Operational");

        Assert.IsNotNull(failure);
        Assert.AreEqual(IPAddress.Parse("192.0.2.140"), failure.SourceAddress);
        Assert.AreEqual(@"CORP\Bob", failure.AccountName);
        Assert.IsFalse(failure.IsCredentialFailure, "CAP 拒絕為授權原則不符，不得標示為密碼失敗");
    }

    /// <summary>
    /// 驗證大量 RD Gateway 201/202/304 授權/原則事件送入關聯引擎時，永不產生密碼噴灑告警 (Negative Test)。
    /// </summary>
    [TestMethod]
    public void Parse_Gateway_StreamOf201_202_304_NeverTriggersSprayAlert()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 10; i++)
        {
            Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Username"] = $@"CORP\User_{i}",
                ["IpAddress"] = "198.51.100.200",
                ["ErrorCode"] = "23003"
            };

            int eventId = (i % 3) switch { 0 => 201, 1 => 202, _ => 304 };
            AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
                fields, now.AddSeconds(i), eventId, "Microsoft-Windows-TerminalServices-Gateway/Operational");

            Assert.IsNotNull(failure);
            Assert.IsFalse(failure.IsCredentialFailure);

            SecurityObservationEvent obs = new()
            {
                SourceAgentName = "RdGatewaySecurityAgent",
                ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
                NormalizedIpAddress = failure.SourceAddress.ToString(),
                NormalizedAccount = failure.AccountName,
                EventTimeUtc = failure.OccurredAt,
                IsCredentialFailure = failure.IsCredentialFailure
            };

            CorrelationEvaluationResult result = engine.Evaluate(obs);
            Assert.AreEqual(CorrelationAction.None, result.Action, "授權/原則拒絕事件絕不可觸發噴灑告警");
            Assert.AreEqual(SprayAttackType.None, result.SprayType);
        }
    }

    /// <summary>
    /// 驗證 TerminalServices-Gateway Event 202 (RAP 拒絕) 能正確解析目標資源且標記為非密碼失敗。
    /// </summary>
    [TestMethod]
    public void Parse_Gateway_Event202_RapDenied_Success()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username"] = @"CORP\Alice",
            ["IpAddress"] = "2001:db8:85a3::8a2e:370:7334",
            ["Resource"] = "SRV-FINANCE-01",
            ["ErrorCode"] = "23004"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, now, 202, "Microsoft-Windows-TerminalServices-Gateway/Operational");

        Assert.IsNotNull(failure);
        Assert.AreEqual(IPAddress.Parse("2001:db8:85a3::8a2e:370:7334"), failure.SourceAddress);
        Assert.IsFalse(failure.IsCredentialFailure);
        StringAssert.Contains(failure.Reason, "SRV-FINANCE-01");
    }

    /// <summary>
    /// 驗證 NPS Event 6273 (ReasonCode 16) 為明確憑證失敗，計入密碼噴灑。
    /// </summary>
    [TestMethod]
    public void Parse_Nps_Event6273_ReasonCode16_IsCredentialFailure()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountName"] = @"CORP\Dave",
            ["CallingStationID"] = "198.51.100.22",
            ["ReasonCode"] = "16"
        };

        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 6273, "Security");

        Assert.IsNotNull(failure);
        Assert.IsTrue(failure.IsCredentialFailure, "ReasonCode 16 應為明確密碼/憑證失敗");
        Assert.AreEqual(1.0, failure.ConfidenceScore);
    }

    /// <summary>
    /// 驗證 NPS Event 6273 (ReasonCode 23) 在 EAP-TLS 憑證驗證失敗時，預設為 Telemetry-only (IsCredentialFailure=false)。
    /// </summary>
    [TestMethod]
    public void Parse_Nps_Event6273_ReasonCode23_EapTls_IsTelemetryOnly()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountName"] = @"CORP\Dave",
            ["CallingStationID"] = "198.51.100.22",
            ["ReasonCode"] = "23",
            ["EAPType"] = "13", // EAP-TLS 憑證認證
            ["EapFriendlyName"] = "Smart Card or other certificate"
        };

        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 6273, "Security");

        Assert.IsNotNull(failure);
        Assert.IsFalse(failure.IsCredentialFailure, "EAP-TLS 憑證問題不應計為密碼錯誤");
    }

    /// <summary>
    /// 驗證 NPS Event 6273 (ReasonCode 23) 僅有 PEAP 外層資訊時，維持僅供遙測使用。
    /// </summary>
    [TestMethod]
    public void Parse_Nps_Event6273_ReasonCode23_PeapWithoutInnerMethod_IsTelemetryOnly()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountName"] = @"CORP\Dave",
            ["CallingStationID"] = "198.51.100.22",
            ["ReasonCode"] = "23",
            ["EAPType"] = "25", // PEAP
            ["EapFriendlyName"] = "Protected EAP (PEAP)"
        };

        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 6273, "Security");

        Assert.IsNotNull(failure);
        Assert.IsFalse(failure.IsCredentialFailure, "PEAP 僅代表外層通道，未證明內層為密碼型驗證時不得計入密碼噴灑");
    }

    /// <summary>
    /// 驗證 NPS Event 6273 (ReasonCode 23) 明確指出內層 MS-CHAPv2 時，才給予低信心關聯。
    /// </summary>
    [TestMethod]
    public void Parse_Nps_Event6273_ReasonCode23_ExplicitMsChapV2_IsLowConfidenceCredentialFailure()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AccountName"] = @"CORP\Dave",
            ["CallingStationID"] = "198.51.100.22",
            ["ReasonCode"] = "23",
            ["AuthenticationType"] = "MS-CHAPv2"
        };

        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 6273, "Security");

        Assert.IsNotNull(failure);
        Assert.IsTrue(failure.IsCredentialFailure, "明確的 MS-CHAPv2 驗證失敗應標示為低信心憑證失敗");
        Assert.AreEqual(0.5, failure.ConfidenceScore, "ReasonCode 23 應採低信心評分");
    }

    /// <summary>
    /// 驗證 Event 200 與 Event 300 授權成功事件不被作為失敗事件輸出。
    /// </summary>
    [TestMethod]
    public void Parse_Gateway_Event200_300_SuccessEvents_ReturnsNull()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username"] = @"CORP\Charlie",
            ["IpAddress"] = "192.0.2.150"
        };

        AuthenticationFailureEvent? failure200 = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 200, "Microsoft-Windows-TerminalServices-Gateway/Operational");
        Assert.IsNull(failure200);

        AuthenticationFailureEvent? failure300 = RdGatewayEventParser.TryParseFields(
            fields, DateTimeOffset.UtcNow, 300, "Microsoft-Windows-TerminalServices-Gateway/Operational");
        Assert.IsNull(failure300);
    }

    /// <summary>
    /// 驗證受信任代理環境下從 X-Forwarded-For 正確還原 RD Gateway 真實訪客 IP。
    /// </summary>
    [TestMethod]
    public void Parse_NatAndProxy_HopAwareClientResolution()
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Username"] = "ext_user",
            ["IpAddress"] = "172.16.0.10",
            ["X-Forwarded-For"] = "198.51.100.77, 172.16.0.10",
            ["ErrorCode"] = "23003"
        };

        string[] trustedProxies = ["172.16.0.0/12"];
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? failure = RdGatewayEventParser.TryParseFields(
            fields, now, 201, "Microsoft-Windows-TerminalServices-Gateway/Operational", trustedProxies);

        Assert.IsNotNull(failure);
        Assert.AreEqual(IPAddress.Parse("198.51.100.77"), failure.SourceAddress);
    }

    /// <summary>
    /// 驗證 Agent 不執行任何本機重複門檻篩選，所有事件均直接向上拋出給中央管線。
    /// </summary>
    [TestMethod]
    public void Agent_DisablesLocalThresholdDetector_EmitsEveryObservationDirectly()
    {
        TestAuthenticationEventSource source = new();
        RdGatewaySecurityAgent agent = new(source);

        try
        {
            int emittedCount = 0;
            agent.AttackDetected += (sender, args) =>
            {
                emittedCount++;
            };

            agent.Start();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            source.Emit(new AuthenticationFailureEvent(now, IPAddress.Parse("192.0.2.80"), 201, "RDGateway", "userA", "CAP Denied"));
            Assert.AreEqual(1, emittedCount, "每筆事件應直接輸出，不被本機門檻 drop");

            source.Emit(new AuthenticationFailureEvent(now.AddSeconds(1), IPAddress.Parse("192.0.2.80"), 201, "RDGateway", "userA", "CAP Denied"));
            Assert.AreEqual(2, emittedCount, "每筆事件均精確輸出一次至管線");
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

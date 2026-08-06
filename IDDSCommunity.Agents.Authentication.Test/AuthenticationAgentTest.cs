using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.IisAuthentication;
using IDDSCommunity.Agents.OpenSsh;
using IDDSCommunity.Agents.PostgreSql;
using IDDSCommunity.Agents.Radius;
using IDDSCommunity.Agents.WindowsNetworkLogon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.Authentication.Test;

[TestClass]
public sealed class AuthenticationAgentTest
{
    [TestMethod]
    public void ThresholdDetectorTriggersAtBoundaryAndResets()
    {
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration { FailureThreshold = 3, WindowSeconds = 60 });
        DateTimeOffset now = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.10", now)));
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.10", now.AddSeconds(1))));
        Assert.IsTrue(detector.Analyze(Failure("192.0.2.10", now.AddSeconds(2))));
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.10", now.AddSeconds(3))));
    }

    [TestMethod]
    public void ThresholdDetectorDoesNotCountDuplicateEvents()
    {
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration { FailureThreshold = 2, WindowSeconds = 60 });
        AuthenticationFailureEvent duplicate = Failure("192.0.2.10", DateTimeOffset.UtcNow);
        Assert.IsFalse(detector.Analyze(duplicate));
        Assert.IsFalse(detector.Analyze(duplicate));
        Assert.IsTrue(detector.Analyze(Failure("192.0.2.10", duplicate.OccurredAt.AddSeconds(1))));
    }

    [TestMethod]
    public void ThresholdDetectorExcludesIpv4Ipv6AndMappedCidrNetworks()
    {
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration
        {
            FailureThreshold = 2,
            WindowSeconds = 60,
            ExcludedAddresses = "192.0.2.0/24;2001:db8::/32;::ffff:198.51.100.0/120"
        });
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (string address in new[] { "192.0.2.20", "2001:db8:1::20", "198.51.100.20" })
        {
            Assert.IsFalse(detector.Analyze(Failure(address, now)));
            Assert.IsFalse(detector.Analyze(Failure(address, now.AddSeconds(1))));
        }
        Assert.IsFalse(detector.Analyze(Failure("203.0.113.20", now)));
        Assert.IsTrue(detector.Analyze(Failure("203.0.113.20", now.AddSeconds(1))));
    }

    [TestMethod]
    public void AuthenticationConfigurationRejectsInvalidCidrNetwork()
    {
        AuthenticationAgentConfiguration configuration = new() { ExcludedAddresses = "192.0.2.0/99" };
        Assert.ThrowsExactly<InvalidOperationException>(configuration.Validate);
    }

    [TestMethod]
    public void ThresholdDetectorEvictsInactiveSourcesByObservationTime()
    {
        ManualTimeProvider time = new(new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration
        {
            FailureThreshold = 3,
            WindowSeconds = 60,
            SourceStateRetentionSeconds = 120
        }, time);
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.1", time.GetUtcNow())));
        Assert.AreEqual(1, detector.TrackedSourceCount);
        time.UtcNow = time.UtcNow.AddSeconds(119);
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.2", time.GetUtcNow())));
        Assert.AreEqual(2, detector.TrackedSourceCount);
        time.UtcNow = time.UtcNow.AddSeconds(2);
        Assert.IsFalse(detector.Analyze(Failure("192.0.2.3", time.GetUtcNow())));
        Assert.AreEqual(2, detector.TrackedSourceCount);
    }

    [TestMethod]
    public void ThresholdDetectorCapacityRemainsBounded()
    {
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration
        {
            FailureThreshold = 3,
            WindowSeconds = 60,
            MaximumTrackedSources = 100
        });
        DateTimeOffset now = DateTimeOffset.UtcNow;
        for (int index = 1; index <= 120; index++)
            Assert.IsFalse(detector.Analyze(Failure($"198.51.100.{index}", now.AddMilliseconds(index))));
        Assert.AreEqual(100, detector.TrackedSourceCount);
    }

    [TestMethod]
    public void PollingLogSourceRetainsPartialLineAndDetectsOverwrite()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-auth-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "authentication.log");
        try
        {
            File.WriteAllText(path, "historical\nstartup-partial");
            List<string> parsed = [];
            List<Exception> errors = [];
            using PollingLogFileFailureSource source = new(() => [path], line => { parsed.Add(line); return null; });
            source.Error += errors.Add;
            source.ReadAvailableForTest();
            File.AppendAllText(path, "-complete\n");
            source.ReadAvailableForTest();
            CollectionAssert.AreEqual(new[] { "startup-partial-complete" }, parsed);
            File.AppendAllText(path, "partial");
            source.ReadAvailableForTest();
            Assert.AreEqual(1, parsed.Count);
            File.AppendAllText(path, "-complete\r\nsecond\n");
            source.ReadAvailableForTest();
            CollectionAssert.AreEqual(new[] { "startup-partial-complete", "partial-complete", "second" }, parsed);
            source.ReadAvailableForTest();
            Assert.AreEqual(3, parsed.Count);

            File.WriteAllText(path, "replacement-line-that-is-longer-than-the-old-offset\n");
            source.ReadAvailableForTest();
            Assert.AreEqual("replacement-line-that-is-longer-than-the-old-offset", parsed[^1]);
            Assert.AreEqual(0, errors.Count);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void OpenSshParserSupportsIpv4AndInvalidUser()
    {
        AuthenticationFailureEvent? failure = OpenSshSecurityAgent.TryParseMessage("Failed password for invalid user admin from 203.0.113.8 port 50000 ssh2", DateTimeOffset.UtcNow);
        Assert.IsNotNull(failure);
        Assert.AreEqual("203.0.113.8", failure.SourceAddress.ToString());
        Assert.AreEqual("admin", failure.AccountName);
    }

    [TestMethod]
    public void NetworkLogonParserRejectsNonCredentialFailures()
    {
        Dictionary<string, string> valid = new() { ["LogonType"] = "3", ["Status"] = "0xC000006D", ["SubStatus"] = "0xC000006A", ["IpAddress"] = "198.51.100.10", ["TargetUserName"] = "service" };
        Assert.IsNotNull(WindowsNetworkLogonSecurityAgent.TryParseFields(valid, DateTimeOffset.UtcNow));
        valid["SubStatus"] = "0xC000015B"; valid["Status"] = "0xC000015B";
        Assert.IsNull(WindowsNetworkLogonSecurityAgent.TryParseFields(valid, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void PostgreSqlParserRequiresFailureAndSourceAddress()
    {
        AuthenticationFailureEvent? failure = PostgreSqlSecurityAgent.TryParseLine("2026-08-05 host=192.0.2.20 FATAL: password authentication failed for user \"postgres\"");
        Assert.IsNotNull(failure);
        Assert.AreEqual("postgres", failure.AccountName);
        Assert.IsNull(PostgreSqlSecurityAgent.TryParseLine("FATAL: password authentication failed for user \"postgres\""));
        AuthenticationFailureEvent? json = PostgreSqlSecurityAgent.TryParseLine("{\"timestamp\":\"2026-08-05T03:04:05Z\",\"user\":\"postgres\",\"remote_host\":\"192.0.2.21\",\"message\":\"password authentication failed for user postgres\"}");
        Assert.IsNotNull(json);
        Assert.AreEqual("192.0.2.21", json.SourceAddress.ToString());
    }

    [TestMethod]
    public void RadiusParserRequiresCredentialMismatchReason()
    {
        Dictionary<string, string> fields = new() { ["ReasonCode"] = "16", ["ClientIPAddress"] = "203.0.113.21", ["UserName"] = "vpn-user" };
        Assert.IsNotNull(RadiusSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
        fields["ReasonCode"] = "48";
        Assert.IsNull(RadiusSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void IisParserUsesW3cFieldDeclarationAnd401Substatus()
    {
        IisW3cAuthenticationParser parser = new();
        Assert.IsNull(parser.Parse("#Fields: date time c-ip cs-username cs-uri-stem sc-status sc-substatus"));
        AuthenticationFailureEvent? failure = parser.Parse("2026-08-05 03:04:05 192.0.2.30 user /owa 401 1");
        Assert.IsNotNull(failure);
        Assert.AreEqual("192.0.2.30", failure.SourceAddress.ToString());
        Assert.IsNull(parser.Parse("2026-08-05 03:04:06 192.0.2.30 user /file 401 3"));
        IisW3cAuthenticationParser filtered = new("/owa");
        _ = filtered.Parse("#Fields: date time c-ip cs-username cs-uri-stem sc-status sc-substatus");
        Assert.IsNull(filtered.Parse("2026-08-05 03:04:06 192.0.2.30 user /login 401 1"));
    }

    private static AuthenticationFailureEvent Failure(string address, DateTimeOffset time) => new(time, IPAddress.Parse(address), 1, "test", string.Empty, string.Empty);

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

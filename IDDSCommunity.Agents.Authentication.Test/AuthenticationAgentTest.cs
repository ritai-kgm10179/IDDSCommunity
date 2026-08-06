using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.IisAuthentication;
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
    public void ThresholdDetectorLeavesAddressPolicyToProtectionService()
    {
        AuthenticationThresholdDetector detector = new(new AuthenticationAgentConfiguration { FailureThreshold = 2, WindowSeconds = 60 });
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.IsFalse(detector.Analyze(Failure("127.0.0.1", now)));
        Assert.IsTrue(detector.Analyze(Failure("127.0.0.1", now.AddSeconds(1))));
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

    [TestMethod]
    public void IisParserIsolatesFieldDeclarationsByLogFile()
    {
        IisW3cAuthenticationParser parser = new();
        _ = parser.Parse("site-a.log", "#Fields: date time c-ip cs-username cs-uri-stem sc-status sc-substatus");
        _ = parser.Parse("site-b.log", "#Fields: sc-status c-ip date time cs-uri-stem cs-username sc-substatus");
        Assert.IsNotNull(parser.Parse("site-a.log", "2026-08-05 03:04:05 192.0.2.30 user /owa 401 1"));
        Assert.IsNotNull(parser.Parse("site-b.log", "401 198.51.100.20 2026-08-05 03:04:05 /login user 0"));
        parser.Reset("site-b.log");
        Assert.IsNull(parser.Parse("site-b.log", "401 198.51.100.20 2026-08-05 03:04:05 /login user 0"));
    }

    private static AuthenticationFailureEvent Failure(string address, DateTimeOffset time) => new(time, IPAddress.Parse(address), 1, "test", string.Empty, string.Empty);

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

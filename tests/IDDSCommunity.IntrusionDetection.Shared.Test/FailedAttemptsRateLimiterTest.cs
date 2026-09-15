using System;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class FailedAttemptsRateLimiterTest
{
    [TestMethod]
    public void Cleanup_DoesNotRescanWithinFiveMinutes_WhenMoreThanFiveThousandSourcesExist()
    {
        FailedAttemptsRateLimiter limiter = new();
        FieldInfo field = typeof(FailedAttemptsRateLimiter).GetField("lastCleanupUtc", BindingFlags.NonPublic | BindingFlags.Instance)!;
        DateTime before = (DateTime)field.GetValue(limiter)!;
        for (int i = 0; i < 6001; i++) limiter.RecordFailedAttempt($"source-{i}");
        for (int i = 0; i < 100; i++) limiter.RecordFailedAttempt($"extra-{i}");
        Assert.AreEqual(before, (DateTime)field.GetValue(limiter)!);
    }

    [TestMethod]
    public void ParallelFailures_ReachThresholdAndLockExpires()
    {
        FailedAttemptsRateLimiter limiter = new(maxFailedAttempts: 10,
            windowDuration: TimeSpan.FromSeconds(2), lockDuration: TimeSpan.FromMilliseconds(100));
        Parallel.For(0, 40, _ => limiter.RecordFailedAttempt("198.51.100.77"));
        Assert.IsTrue(limiter.IsBlocked("198.51.100.77", out _));
        Thread.Sleep(150);
        Assert.IsFalse(limiter.IsBlocked("198.51.100.77", out _));
    }
    [TestMethod]
    public void IsBlocked_AllowsWithinThreshold_BlocksWhenExceeded()
    {
        var limiter = new FailedAttemptsRateLimiter(maxFailedAttempts: 3, windowDuration: TimeSpan.FromMinutes(5), lockDuration: TimeSpan.FromMinutes(10));
        string testIp = "198.51.100.25";

        Assert.IsFalse(limiter.IsBlocked(testIp, out _));

        limiter.RecordFailedAttempt(testIp);
        Assert.IsFalse(limiter.IsBlocked(testIp, out _));

        limiter.RecordFailedAttempt(testIp);
        Assert.IsFalse(limiter.IsBlocked(testIp, out _));

        // 3rd attempt triggers block
        limiter.RecordFailedAttempt(testIp);
        Assert.IsTrue(limiter.IsBlocked(testIp, out TimeSpan retryAfter));
        Assert.IsTrue(retryAfter > TimeSpan.Zero);
        Assert.IsTrue(retryAfter <= TimeSpan.FromMinutes(10));

        // Other IP is unaffected
        Assert.IsFalse(limiter.IsBlocked("203.0.113.10", out _));

        // Reset clears block
        limiter.Reset(testIp);
        Assert.IsFalse(limiter.IsBlocked(testIp, out _));
    }

    [TestMethod]
    public void IsBlocked_HandlesEmptyAndNullGracefully()
    {
        var limiter = new FailedAttemptsRateLimiter();
        Assert.IsFalse(limiter.IsBlocked(null, out _));
        Assert.IsFalse(limiter.IsBlocked(string.Empty, out _));
        Assert.IsFalse(limiter.IsBlocked("   ", out _));

        limiter.RecordFailedAttempt(null);
        limiter.RecordFailedAttempt(string.Empty);
        limiter.Reset(null);
    }
}

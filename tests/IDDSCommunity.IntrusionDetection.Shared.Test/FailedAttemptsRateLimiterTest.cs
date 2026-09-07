using System;
using System.Threading;
using IDDSCommunity.IntrusionDetection.Shared.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class FailedAttemptsRateLimiterTest
{
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
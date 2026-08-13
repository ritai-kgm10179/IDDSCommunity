using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class SecurityLogRefreshPolicyTest
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Local);

    [TestMethod]
    public void ShouldRefreshReturnsTrueForNewSequenceEvenWhenEventIsOutsideDisplayWindow() =>
        Assert.IsTrue(SecurityLogRefreshPolicy.ShouldRefresh(Now, Now.AddSeconds(-1), 100, 101, Interval));

    [TestMethod]
    public void ShouldRefreshReturnsTrueWhenSequenceMovesBackwardAfterRestore() =>
        Assert.IsTrue(SecurityLogRefreshPolicy.ShouldRefresh(Now, Now.AddSeconds(-1), 1000, 500, Interval));

    [TestMethod]
    public void ShouldRefreshUsesInclusiveRefreshIntervalBoundary()
    {
        Assert.IsFalse(SecurityLogRefreshPolicy.ShouldRefresh(Now, Now.AddTicks(-Interval.Ticks + 1), 100, 100, Interval));
        Assert.IsTrue(SecurityLogRefreshPolicy.ShouldRefresh(Now, Now.Subtract(Interval), 100, 100, Interval));
    }

    [TestMethod]
    public void ShouldRefreshRecoversFromClockRollback() =>
        Assert.IsTrue(SecurityLogRefreshPolicy.ShouldRefresh(Now, Now.AddMinutes(1), 100, 100, Interval));

    [TestMethod]
    public void ShouldRefreshRejectsNonPositiveInterval()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SecurityLogRefreshPolicy.ShouldRefresh(Now, Now, 0, 0, TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SecurityLogRefreshPolicy.ShouldRefresh(Now, Now, 0, 0, TimeSpan.FromTicks(-1)));
    }
}

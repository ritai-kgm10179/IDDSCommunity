using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class LockoutPolicyTest
{
    [TestMethod]
    public void CalculateSoftLockMinutesUsesBoundedExponentialBackoff()
    {
        Assert.AreEqual(1, LockoutPolicy.CalculateSoftLockMinutes(1, 0));
        Assert.AreEqual(2, LockoutPolicy.CalculateSoftLockMinutes(1, 1));
        Assert.AreEqual(4, LockoutPolicy.CalculateSoftLockMinutes(1, 2));
        Assert.AreEqual(32, LockoutPolicy.CalculateSoftLockMinutes(1, 5));
        Assert.AreEqual(64, LockoutPolicy.CalculateSoftLockMinutes(1, 6));
        Assert.AreEqual(43200, LockoutPolicy.CalculateSoftLockMinutes(1, 20));
        Assert.AreEqual(43200, LockoutPolicy.CalculateSoftLockMinutes(1, int.MaxValue));

        // 測試自訂上限
        Assert.AreEqual(60, LockoutPolicy.CalculateSoftLockMinutes(1, 6, 60));
        Assert.AreEqual(60, LockoutPolicy.CalculateSoftLockMinutes(1, int.MaxValue, 60));
        Assert.AreEqual(1440, LockoutPolicy.CalculateSoftLockMinutes(5, 10, 1440));
    }

    [TestMethod]
    public void CalculateSoftLockMinutesRejectsInvalidInputs()
    {
        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => LockoutPolicy.CalculateSoftLockMinutes(0, 0));
        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => LockoutPolicy.CalculateSoftLockMinutes(1, -1));
    }
}

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
        Assert.AreEqual(60, LockoutPolicy.CalculateSoftLockMinutes(1, 6));
        Assert.AreEqual(60, LockoutPolicy.CalculateSoftLockMinutes(1, int.MaxValue));
    }

    [TestMethod]
    public void CalculateSoftLockMinutesRejectsInvalidInputs()
    {
        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => LockoutPolicy.CalculateSoftLockMinutes(0, 0));
        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => LockoutPolicy.CalculateSoftLockMinutes(1, -1));
    }
}

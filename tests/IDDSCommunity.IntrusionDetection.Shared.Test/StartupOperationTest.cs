using System;
using IDDSCommunity.IntrusionDetection.Admin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class StartupOperationTest
{
    /// <summary>
    /// Verifies that successful startup reaches a terminal success state and cannot run twice.
    /// </summary>
    [TestMethod]
    public void TryRun_Success_CompletesOnce()
    {
        StartupOperation operation = new();
        int calls = 0;

        Assert.IsTrue(operation.TryRun(() => calls++, out Exception? failure));
        Assert.IsNull(failure);
        Assert.IsTrue(operation.Succeeded);
        Assert.IsFalse(operation.TryRun(() => calls++, out _));
        Assert.AreEqual(1, calls);
    }
    /// <summary>
    /// Verifies that startup failure is captured as a terminal result instead of leaving the splash loop active.
    /// </summary>
    [TestMethod]
    public void TryRun_Failure_IsCapturedAndDoesNotRepeat()
    {
        StartupOperation operation = new();

        Assert.IsTrue(operation.TryRun(static () => throw new InvalidOperationException("expected"), out Exception? failure));
        Assert.IsInstanceOfType<InvalidOperationException>(failure);
        Assert.IsFalse(operation.Succeeded);
        Assert.IsFalse(operation.TryRun(static () => { }, out _));
    }
}

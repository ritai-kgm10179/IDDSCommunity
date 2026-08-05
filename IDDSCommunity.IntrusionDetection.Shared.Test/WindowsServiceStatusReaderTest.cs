using System;
using System.ComponentModel;
using System.ServiceProcess;
using IDDSCommunity.IntrusionDetection.Admin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class WindowsServiceStatusReaderTest
{
    /// <summary>
    /// Verifies that a valid service status is returned without a failure.
    /// </summary>
    [TestMethod]
    public void TryRead_ReturnsObservedStatus()
    {
        bool success = WindowsServiceStatusReader.TryRead(
            static () => ServiceControllerStatus.Running,
            out ServiceControllerStatus status,
            out Exception? failure);

        Assert.IsTrue(success);
        Assert.AreEqual(ServiceControllerStatus.Running, status);
        Assert.IsNull(failure);
    }

    /// <summary>
    /// Verifies that a missing-service exception becomes an unavailable result instead of escaping a timer callback.
    /// </summary>
    [TestMethod]
    public void TryRead_InvalidOperationException_ReturnsUnavailable()
    {
        bool success = WindowsServiceStatusReader.TryRead(
            static () => throw new InvalidOperationException("missing service"),
            out _,
            out Exception? failure);

        Assert.IsFalse(success);
        Assert.IsInstanceOfType<InvalidOperationException>(failure);
    }

    /// <summary>
    /// Verifies that a Windows service-control error becomes an unavailable result.
    /// </summary>
    [TestMethod]
    public void TryRead_Win32Exception_ReturnsUnavailable()
    {
        bool success = WindowsServiceStatusReader.TryRead(
            static () => throw new Win32Exception(1060),
            out _,
            out Exception? failure);

        Assert.IsFalse(success);
        Assert.IsInstanceOfType<Win32Exception>(failure);
    }
}

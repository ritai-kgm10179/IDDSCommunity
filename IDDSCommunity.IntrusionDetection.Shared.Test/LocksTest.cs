using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public class LocksTest
{

    /// <summary>
    /// Initializes a new instance of the <see cref="LocksTest"/> class.
    /// </summary>

    public LocksTest() => Database.Instance.Configure(System.Windows.Forms.Application.StartupPath);

    /// <summary>
    /// Creates lock test.
    /// </summary>

    [TestMethod]
    public void CreateLockTest()
    {
        long currentMaxId = GetMaxLocksId();
        Lock l = new()
        {
            IpAddress = "10.20.1.1",
            LockDate = DateTime.Now,
            UnlockDate = DateTime.Now.AddDays(1),
            Port = 0,
            Status = Lock.LOCK_STATUS_HARDLOCK,
            NumberOfSoftLocks = 2,
            TriggerIncident = 100
        };
        l.Id = Locks.CreateLock(l);
        Assert.AreEqual(currentMaxId + 1, l.Id);
    }

    /// <summary>
    /// Gets max locks id.
    /// </summary>
    /// <returns>The get max locks id result.</returns>

    private static long GetMaxLocksId()
    {
        object? result = Database.Instance.ExecuteScalar("Select max(LockId) from Locks");
        return Db.DbValueConverter.ToInt64(result);

    }

    /// <summary>
    /// Executes the test lock exists operation.
    /// </summary>

    [TestMethod]
    public void TestLockExists() => Assert.IsFalse(Locks.LockExists("192.158.178.120"));

    /// <summary>
    /// Executes the test lock exists2 operation.
    /// </summary>

    [TestMethod]
    public void TestLockExists2() => Assert.IsTrue(Locks.LockExists("10.20.1.1"));
}

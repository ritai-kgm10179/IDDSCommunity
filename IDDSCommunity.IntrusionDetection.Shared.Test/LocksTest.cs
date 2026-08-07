using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public class LocksTest
{
    /// <summary>
    /// 初始化 <see cref="LocksTest"/> 類別的新執行個體。
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
    /// <returns>傳回 get max locks id 的結果。</returns>

    private static long GetMaxLocksId()
    {
        object? result = Database.Instance.ExecuteScalar("Select max(LockId) from Locks");
        return Db.DbValueConverter.ToInt64(result);

    }
    /// <summary>
    /// 執行 test lock exists 作業。
    /// </summary>

    [TestMethod]
    public void TestLockExists() => Assert.IsFalse(Locks.LockExists("192.158.178.120"));
    /// <summary>
    /// 執行 test lock exists2 作業。
    /// </summary>

    [TestMethod]
    public void TestLockExists2() => Assert.IsTrue(Locks.LockExists("10.20.1.1"));
}

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
            LockDate = DateTime.UtcNow,
            UnlockDate = DateTime.UtcNow.AddDays(1),
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

    /// <summary>
    /// 驗證當鎖定記錄關聯之入侵日誌或代理程式不存在時，ReadLocks 依然能正確讀取封鎖記錄與 IP 位址。
    /// </summary>
    [TestMethod]
    public void ReadLocks_WithOrphanedIncidentOrMissingAgent_ReturnsLockRecord()
    {
        string targetIp = "198.51.100.55";
        Lock l = new()
        {
            IpAddress = targetIp,
            LockDate = DateTime.UtcNow,
            UnlockDate = DateTime.UtcNow.AddDays(1),
            Port = 25,
            Status = Lock.LOCK_STATUS_HARDLOCK,
            NumberOfSoftLocks = 0,
            TriggerIncident = 99999999 // 不存在於 IntrusionLog 之虛擬識別碼
        };
        l.Id = Locks.CreateLock(l);

        bool found = false;
        using System.Data.IDataReader reader = Locks.ReadLocks();
        while (reader.Read())
        {
            long lockId = Db.DbValueConverter.ToInt64(reader["LockId"]);
            if (lockId == l.Id)
            {
                found = true;
                string clientIp = Db.DbValueConverter.ToString(reader["ClientIp"]);
                int status = Db.DbValueConverter.ToInt(reader["Status"]);
                Assert.AreEqual(targetIp, clientIp);
                Assert.AreEqual(Lock.LOCK_STATUS_HARDLOCK, status);
                break;
            }
        }

        Assert.IsTrue(found, "即使 TriggerIncident 不存在於 IntrusionLog，ReadLocks 仍應透過 LEFT JOIN 與 COALESCE 成功傳回該筆封鎖記錄。");
    }

    /// <summary>
    /// 驗證當鎖定記錄之 LastUpdate 為 NULL 時，HasUpdates 仍能藉由 LockDate 正確偵測到更新。
    /// </summary>
    [TestMethod]
    public void HasUpdates_WithNullLastUpdate_DetectsUpdate()
    {
        Lock l = new()
        {
            IpAddress = "198.51.100.66",
            LockDate = DateTime.UtcNow,
            UnlockDate = DateTime.UtcNow.AddDays(1),
            Port = 80,
            Status = Lock.LOCK_STATUS_HARDLOCK,
            NumberOfSoftLocks = 0,
            TriggerIncident = 0
        };
        l.Id = Locks.CreateLock(l);

        // 明確將 LastUpdate 清為 NULL 模擬舊版或未填寫情境
        Database.Instance.ExecuteNonQuery("UPDATE Locks SET LastUpdate = NULL WHERE LockId = @p0", l.Id);

        bool hasUpdates = Locks.HasUpdates(DateTime.UtcNow.AddMinutes(-5));
        Assert.IsTrue(hasUpdates, "當 LastUpdate 為 NULL 時，應能藉由 LockDate 成功判斷存在更新。");
    }

    /// <summary>
    /// 驗證 ReadLocks 能自關聯之 IntrusionLog 正確傳回 AgentId。
    /// </summary>
    [TestMethod]
    public void ReadLocks_WithIncidentAgentId_ReturnsAgentId()
    {
        string targetIp = "198.51.100.77";
        Guid agentGuid = WellKnownAgentIds.WindowsNetworkLogon;
        long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, agentGuid, targetIp, IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);

        Lock l = new()
        {
            IpAddress = targetIp,
            LockDate = DateTime.UtcNow,
            UnlockDate = DateTime.UtcNow.AddDays(1),
            Port = 445,
            Status = Lock.LOCK_STATUS_HARDLOCK,
            NumberOfSoftLocks = 0,
            TriggerIncident = incidentId
        };
        l.Id = Locks.CreateLock(l);

        bool found = false;
        using System.Data.IDataReader reader = Locks.ReadLocks();
        while (reader.Read())
        {
            long lockId = Db.DbValueConverter.ToInt64(reader["LockId"]);
            if (lockId == l.Id)
            {
                found = true;
                string readAgentId = Db.DbValueConverter.ToString(reader["AgentId"]);
                Assert.IsTrue(Guid.TryParse(readAgentId, out Guid parsedGuid) && parsedGuid == agentGuid);
                break;
            }
        }

        Assert.IsTrue(found, "ReadLocks 應成功傳回包含關聯 IntrusionLog 之 AgentId。");
    }
}

using System;
using System.Collections.Generic;
using System.Data;


namespace IDDSCommunity.IntrusionDetection.Shared;

public class Locks
{


    /// <summary>
    /// Determines whether s updates.
    /// </summary>
    /// <param name="lastUpdate">last update參數。</param>
    /// <returns>若s updates傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool HasUpdates(DateTime lastUpdate)
    {
        if (Database.Instance.IsConfigured)
        {
            object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where LastUpdate>@p0", lastUpdate);
            if (result != null && int.TryParse(result.ToString(), out int count))
            {
                return count > 0;
            }
            else
            {
                return false;
            }
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Returns every database lock whose desired state is an active firewall block.
    /// </summary>
    /// <returns>傳回 active lock records 的結果。</returns>
    public static List<Lock> GetActiveLocks()
    {
        List<Lock> result = [];
        using IDataReader reader = Database.Instance.ExecuteReader(
            "select * from Locks where status in (@p0,@p1,@p2,@p3)",
            Lock.LOCK_STATUS_HARDLOCK,
            Lock.LOCK_STATUS_SOFTLOCK,
            Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
            Lock.LOCK_STATUS_SOFTLOCK_REQUESTED);
        while (reader.Read())
        {
            result.Add(new Lock
            {
                Id = Db.DbValueConverter.ToInt64(reader["LockId"]),
                IpAddress = Db.DbValueConverter.ToString(reader["IpAddress"]),
                LockDate = Db.DbValueConverter.ToDateTime(reader["LockDate"]),
                Port = Db.DbValueConverter.ToInt(reader["Port"]),
                Status = Db.DbValueConverter.ToInt(reader["Status"]),
                TriggerIncident = Db.DbValueConverter.ToInt64(reader["TriggerIncident"]),
                UnlockDate = Db.DbValueConverter.ToDateTime(reader["UnlockDate"])
            });
        }
        return result;
    }
    /// <summary>
    /// Reads locks.
    /// </summary>
    /// <returns>傳回read locks結果。</returns>
    public static IDataReader ReadLocks()
    {
        if (Database.Instance.IsConfigured)
        {
            return Database.Instance.ExecuteReader(@"select l.LockId, i.ClientIp, l.LockDate, l.UnlockDate,i.IncidentTime, a.DisplayName, l.status
                                                        from Locks l inner join IntrusionLog i on l.TriggerIncident = i.Id
                                                            inner join SecurityAgents a on i.AgentId = a.AgentId
                                                        where l.status in (@p0,@p1) order by l.LockDate desc", Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// 執行today作業。
    /// </summary>
    /// <returns>傳回today結果。</returns>
    public static int Today()
    {
        if (Database.Instance.IsConfigured)
        {
            object? queryResult = Database.Instance.ExecuteScalar(@"select count(*) from IntrusionLog where (action=@p0 or action=@p1) and IncidentTime>@p2",
                IntrusionLog.STATUS_SOFT_LOCKED, IntrusionLog.STATUS_HARD_LOCKED, DateTime.Now.AddDays(-1));
            if (int.TryParse(queryResult?.ToString(), out int result))
            {
                return result;
            }
            else
            {
                throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Invalid data"));
            }
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Reads current soft locks.
    /// </summary>
    /// <returns>傳回read current soft locks結果。</returns>
    public static int ReadCurrentSoftLocks()
    {
        object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where status in (@p0) ", (int)LockStatus.SoftLocked);
        int.TryParse(result?.ToString(), out int softLocks);
        return softLocks;
    }
    /// <summary>
    /// Reads current hard locks.
    /// </summary>
    /// <returns>傳回read current hard locks結果。</returns>
    public static int ReadCurrentHardLocks()
    {
        object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where status in (@p0)", (int)LockStatus.HardLocked);
        int.TryParse(result?.ToString(), out int hardLocks);
        return hardLocks;
    }
    /// <summary>
    /// 讀取指定半開時間區間內的登入失敗統計快照。
    /// </summary>
    /// <param name="startDate">包含在統計內的開始時間。</param>
    /// <param name="endDate">不包含在統計內的結束時間。</param>
    /// <returns>包含總數及各 Agent 計數的統計快照。</returns>
    public static FailedLoginStatisticsSnapshot ReadFailedLoginStatistics(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "結束時間必須晚於開始時間。");

        Dictionary<Guid, int> attemptsByAgent = [];
        int total = 0;
        using IDataReader reader = Database.Instance.ExecuteReader(
            @"select AgentId, count(*) as Incidents
              from IntrusionLog
              where IncidentTime>=@p0 and IncidentTime<@p1
                and Action in (@p2,@p3,@p4)
              group by AgentId",
            startDate,
            endDate,
            IntrusionLog.STATUS_INTRUSION_ATTEMPT,
            IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL,
            IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE);
        while (reader.Read())
        {
            int incidents = Db.DbValueConverter.ToInt(reader["Incidents"]);
            if (Guid.TryParse(Db.DbValueConverter.ToString(reader["AgentId"]), out Guid agentId))
            {
                attemptsByAgent[agentId] = incidents;
                total += incidents;
            }
        }

        return new FailedLoginStatisticsSnapshot(total, attemptsByAgent);
    }

    /// <summary>
    /// 讀取每個 Agent 的累計封鎖統計資料。
    /// </summary>
    /// <returns>以 Agent 識別碼索引的累計封鎖統計資料。</returns>
    public static IReadOnlyDictionary<Guid, AgentLockStatistics> ReadAgentLockStatistics()
    {
        Dictionary<Guid, AgentLockStatistics> statistics = [];
        using IDataReader reader = Database.Instance.ExecuteReader("select AgentId, HardLocks, SoftLocks from AgentStatistics");
        while (reader.Read())
        {
            if (Guid.TryParse(Db.DbValueConverter.ToString(reader["AgentId"]), out Guid agentId))
            {
                statistics[agentId] = new AgentLockStatistics(
                    Db.DbValueConverter.ToInt(reader["HardLocks"]),
                    Db.DbValueConverter.ToInt(reader["SoftLocks"]));
            }
        }

        return statistics;
    }
    /// <summary>
    /// Counts recent locks created for the same Agent and source IP address.
    /// </summary>
    public static int GetRecentLockCount(Guid agentId, string ipAddress, DateTime startDate)
    {
        object? result = Database.Instance.ExecuteScalar(
            @"select count(*) from Locks l
              inner join IntrusionLog i on l.TriggerIncident = i.Id
              where i.AgentId=@p0 and l.IpAddress=@p1 and l.LockDate>=@p2",
            agentId,
            ipAddress,
            startDate);
        return Db.DbValueConverter.ToInt(result);
    }
    /// <summary>
    /// Gets current locks.
    /// </summary>
    /// <returns>傳回get current locks結果。</returns>
    public static List<Lock> GetCurrentLocks()
    {
        if (Database.Instance.IsConfigured)
        {
            List<Lock> result = [];
            string sqlString = @"select LockId, LockDate, UnlockDate, TriggerIncident, Status, Port, IpAddress from Locks where status in (@p0,@p1)";
            IDataReader rdr = Database.Instance.ExecuteReader(sqlString, Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK);
            while (rdr.Read())
            {
                Lock l = new()
                {
                    Id = Db.DbValueConverter.ToInt64(rdr["LockId"]),
                    LockDate = Db.DbValueConverter.ToDateTime(rdr["LockDate"]),
                    UnlockDate = Db.DbValueConverter.ToDateTime(rdr["UnlockDate"]),
                    Status = Db.DbValueConverter.ToInt(rdr["Status"]),
                    Port = Db.DbValueConverter.ToInt(rdr["Port"]),
                    IpAddress = Db.DbValueConverter.ToString(rdr["IpAddress"])
                };
                result.Add(l);
            }
            rdr.Close();
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Gets lock by id.
    /// </summary>
    /// <param name="id">id參數。</param>
    /// <returns>傳回get lock by id結果。</returns>
    public static Lock GetLockById(long id)
    {
        if (Database.Instance.IsConfigured)
        {
            Lock result = new();
            string sqlString = @"select LockId, LockDate, UnlockDate, TriggerIncident, Status, Port, IpAddress from Locks where LockId = @p0";
            IDataReader rdr = Database.Instance.ExecuteReader(sqlString, id);
            if (rdr.Read())
            {
                result.Id = Db.DbValueConverter.ToInt64(rdr["LockId"]);
                result.LockDate = Db.DbValueConverter.ToDateTime(rdr["LockDate"]);
                result.UnlockDate = Db.DbValueConverter.ToDateTime(rdr["UnlockDate"]);
                result.Status = Db.DbValueConverter.ToInt(rdr["Status"]);
                result.Port = Db.DbValueConverter.ToInt(rdr["Port"]);
                result.IpAddress = Db.DbValueConverter.ToString(rdr["IpAddress"]);
                result.TriggerIncident = Db.DbValueConverter.ToInt64(rdr["TriggerIncident"]);
            }
            rdr.Close();
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// 執行lock exists作業。
    /// </summary>
    /// <param name="ipAddress">ip address參數。</param>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool LockExists(string ipAddress)
    {
        if (Database.Instance.IsConfigured)
        {

            string sqlString = @"Select count(*) from Locks where IpAddress=@p0 and status in (@p1,@p2,@p3,@p4)";

            object? count = Database.Instance.ExecuteScalar(sqlString, ipAddress, Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK, Lock.LOCK_STATUS_SOFTLOCK_REQUESTED, Lock.LOCK_STATUS_HARDLOCK_REQUESTED);
            if (Db.DbValueConverter.ToInt(count) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// 建立鎖定記錄。
    /// </summary>
    /// <param name="lockDate">lock date參數。</param>
    /// <param name="unlockDate">unlock date參數。</param>
    /// <param name="triggerIncident">trigger incident參數。</param>
    /// <param name="status">status參數。</param>
    /// <param name="port">port參數。</param>
    /// <param name="ipAddress">ip address參數。</param>
    /// <returns>傳回create lock結果。</returns>
    public static Lock CreateLock(DateTime lockDate, DateTime unlockDate, long triggerIncident, int status, int port, string ipAddress)
    {
        Lock l = new()
        {
            IpAddress = ipAddress,
            LockDate = lockDate,
            Port = port,
            Status = status,
            TriggerIncident = triggerIncident,
            UnlockDate = unlockDate
        };
        l.Id = CreateLock(l);
        return l;
    }
    /// <summary>
    /// 建立鎖定記錄。
    /// </summary>
    /// <param name="l">l參數。</param>
    /// <returns>傳回create lock結果。</returns>
    public static long CreateLock(Lock l)
    {
        if (Database.Instance.IsConfigured)
        {
            Lock result = new();
            string sqlString = @"insert into Locks(LockDate, UnlockDate, TriggerIncident, Status, Port, IpAddress, LastUpdate) values (@p0,@p1,@p2,@p3,@p4,@p5,@p6) RETURNING LockId";
            object? id = Database.Instance.ExecuteScalar(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.Now);
            l.Id = Db.DbValueConverter.ToInt64(id);
            return l.Id;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// 更新鎖定記錄。
    /// </summary>
    /// <param name="l">l參數。</param>
    public static void UpdateLock(Lock l)
    {
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"update Locks set LockDate=@p0, UnlockDate=@p1, TriggerIncident=@p2, Status=@p3, Port=@p4, IpAddress=@p5, LastUpdate=@p6 where LockId=@p7";
            Database.Instance.ExecuteNonQuery(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.Now, l.Id);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// 取得解鎖清單。
    /// </summary>
    /// <returns>傳回get unlock list結果。</returns>
    public static List<Lock> GetUnlockList()
    {
        List<Lock> result = [];
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"select * from Locks where (UnlockDate<@p0 and (status=@p1 or status=@p2)) or status=@p3";
            IDataReader rdr = Database.Instance.ExecuteReader(sqlString, DateTime.Now, Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK, Lock.LOCK_STATUS_UNLOCK_REQUESTED);
            while (rdr.Read())
            {
                Lock l = new()
                {
                    Id = Db.DbValueConverter.ToInt64(rdr["LockId"]),
                    IpAddress = Db.DbValueConverter.ToString(rdr["IpAddress"]),
                    LockDate = Db.DbValueConverter.ToDateTime(rdr["LockDate"]),
                    Port = Db.DbValueConverter.ToInt(rdr["Port"]),
                    Status = Db.DbValueConverter.ToInt(rdr["Status"]),
                    TriggerIncident = Db.DbValueConverter.ToInt64(rdr["TriggerIncident"]),
                    UnlockDate = Db.DbValueConverter.ToDateTime(rdr["UnlockDate"])
                };
                result.Add(l);
            }
            rdr.Close();
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }



}

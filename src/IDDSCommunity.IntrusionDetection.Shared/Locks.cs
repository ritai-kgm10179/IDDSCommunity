using System;
using System.Collections.Generic;
using System.Data;


namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供 Windows 防火牆封鎖規則同步、自動過期清理與解除封鎖之管理類別。
/// </summary>
public class Locks
{


    /// <summary>
    /// 判斷自指定時間戳記以來資料庫中的封鎖記錄是否有更新。
    /// </summary>
    /// <param name="lastUpdate">前次更新時間戳記。</param>
    /// <returns>若有更新傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool HasUpdates(DateTime lastUpdate)
    {
        if (Database.Instance.IsConfigured)
        {
            object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where coalesce(LastUpdate, LockDate)>@p0", lastUpdate);
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
    /// 讀取目前所有生效中之硬封鎖與軟封鎖記錄。
    /// </summary>
    /// <returns>包含封鎖識別碼、客戶端 IP、封鎖時間、預計解鎖時間、代理程式名稱與狀態之資料讀取器。</returns>
    public static IDataReader ReadLocks()
    {
        if (Database.Instance.IsConfigured)
        {
            return Database.Instance.ExecuteReader(@"select l.LockId, coalesce(nullif(l.IpAddress, ''), i.ClientIp, '') as ClientIp, l.LockDate, l.UnlockDate, coalesce(i.IncidentTime, l.LockDate) as IncidentTime, coalesce(a.DisplayName, '') as DisplayName, coalesce(i.AgentId, '') as AgentId, l.status
                                                        from Locks l left join IntrusionLog i on l.TriggerIncident = i.Id
                                                            left join SecurityAgents a on lower(i.AgentId) = lower(a.AgentId)
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
                IntrusionLog.STATUS_SOFT_LOCKED, IntrusionLog.STATUS_HARD_LOCKED, DateTime.UtcNow.AddDays(-1));
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
            total += incidents;
            if (TryResolveAgentId(Db.DbValueConverter.ToString(reader["AgentId"]), out Guid agentId))
            {
                attemptsByAgent.TryGetValue(agentId, out int existingIncidents);
                attemptsByAgent[agentId] = existingIncidents + incidents;
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
            if (TryResolveAgentId(Db.DbValueConverter.ToString(reader["AgentId"]), out Guid agentId))
            {
                statistics.TryGetValue(agentId, out AgentLockStatistics? existing);
                statistics[agentId] = new AgentLockStatistics(
                    (existing?.HardLocks ?? 0) + Db.DbValueConverter.ToInt(reader["HardLocks"]),
                    (existing?.SoftLocks ?? 0) + Db.DbValueConverter.ToInt(reader["SoftLocks"]));
            }
        }

        return statistics;
    }

    /// <summary>
    /// 讀取指定時間區間內的跨代理程式密碼噴灑告警數量。
    /// </summary>
    /// <param name="startDate">包含在統計內的 UTC 開始時間。</param>
    /// <param name="endDate">不包含在統計內的 UTC 結束時間。</param>
    /// <returns>跨代理程式密碼噴灑告警數量。</returns>
    public static int ReadCrossAgentAlertCount(DateTime startDate, DateTime endDate)
    {
        object? result = Database.Instance.ExecuteScalar(
            @"select count(*) from ProtectionAuditLog
              where OccurredUtc>=@p0 and OccurredUtc<@p1 and EventType=@p2",
            new DateTimeOffset(startDate.ToUniversalTime()).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            new DateTimeOffset(endDate.ToUniversalTime()).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            "CrossAgentSprayDetected");
        return Db.DbValueConverter.ToInt(result);
    }

    private static bool TryResolveAgentId(string persistedAgentId, out Guid agentId)
    {
        if (string.IsNullOrWhiteSpace(persistedAgentId))
        {
            agentId = Guid.Empty;
            return false;
        }

        // 1. 已知的固定 GUID 可直接使用；歷史隨機 GUID 必須先透過 SecurityAgents 的穩定名稱映射。
        if (WellKnownAgentIds.TryResolveCanonicalGuid(persistedAgentId, out agentId) &&
            WellKnownAgentIds.IsWellKnown(agentId))
            return true;

        // 2. 若為資料庫中舊記錄之 AgentId，透過 SecurityAgents 資料表查詢其 Name / DisplayName / AssemblyName 並映射至 Canonical GUID
        try
        {
            if (Database.Instance != null && Database.Instance.IsConfigured)
            {
                using IDataReader reader = Database.Instance.ExecuteReader(
                    @"select Name, DisplayName, AssemblyName, AgentId
                      from SecurityAgents
                      where AgentId=@p0 or Name=@p0 or DisplayName=@p0 or AssemblyName=@p0
                      limit 1",
                    persistedAgentId);
                if (reader.Read())
                {
                    string name = Db.DbValueConverter.ToString(reader["Name"]);
                    string displayName = Db.DbValueConverter.ToString(reader["DisplayName"]);
                    string assemblyName = Db.DbValueConverter.ToString(reader["AssemblyName"]);
                    if (WellKnownAgentIds.TryResolveCanonicalGuid(name, out agentId) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(displayName, out agentId) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(assemblyName, out agentId))
                    {
                        return true;
                    }
                    if (Guid.TryParse(Db.DbValueConverter.ToString(reader["AgentId"]), out agentId))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Fallback to raw parsing
        }

        // 3. 回退至原始 GUID
        if (Guid.TryParse(persistedAgentId, out agentId))
            return true;

        return false;
    }
    /// <summary>
    /// 計算指定代理程式與來源 IP 位址在指定起始時間後的近期封鎖累計次數。
    /// </summary>
    /// <param name="agentId">代理程式識別碼。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="startDate">計算起始時間（UTC）。</param>
    /// <returns>傳回符合條件之近期封鎖次數。</returns>
    public static int GetRecentLockCount(Guid agentId, string ipAddress, DateTime startDate)
    {
        if (!Database.Instance.IsConfigured)
            return 0;

        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);
        object? result = Database.Instance.ExecuteScalar(
            @"select count(*) from Locks l
              where (l.IpAddress=@p0 or exists (select 1 from IntrusionLog i where l.TriggerIncident = i.Id and i.ClientIP=@p0 and i.AgentId=@p1))
                and l.LockDate>=@p2",
            ipAddress,
            agentId,
            startDate);
        return Db.DbValueConverter.ToInt(result);
    }

    /// <summary>
    /// 計算指定來源 IP 位址在指定起始時間後的所有近期封鎖累計次數。
    /// </summary>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="startDate">計算起始時間（UTC）。</param>
    /// <returns>傳回符合條件之近期封鎖次數。</returns>
    public static int GetRecentLockCount(string ipAddress, DateTime startDate)
    {
        if (!Database.Instance.IsConfigured)
            return 0;

        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);
        object? result = Database.Instance.ExecuteScalar(
            @"select count(*) from Locks
              where IpAddress=@p0 and LockDate>=@p1",
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
            using IDataReader rdr = Database.Instance.ExecuteReader(sqlString, Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK);
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
            using IDataReader rdr = Database.Instance.ExecuteReader(sqlString, id);
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

            ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);

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
    /// <param name="lockDate">鎖定建立時間；必須為 UTC。</param>
    /// <param name="unlockDate">預定解鎖時間；必須為 UTC。</param>
    /// <param name="triggerIncident">trigger incident參數。</param>
    /// <param name="status">status參數。</param>
    /// <param name="port">port參數。</param>
    /// <param name="ipAddress">ip address參數。</param>
    /// <returns>傳回create lock結果。</returns>
    public static Lock CreateLock(DateTime lockDate, DateTime unlockDate, long triggerIncident, int status, int port, string ipAddress)
    {
        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);
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
            object? id = Database.Instance.ExecuteScalar(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.UtcNow);
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
            Database.Instance.ExecuteNonQuery(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.UtcNow, l.Id);
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
            using IDataReader rdr = Database.Instance.ExecuteReader(sqlString, DateTime.UtcNow, Lock.LOCK_STATUS_HARDLOCK, Lock.LOCK_STATUS_SOFTLOCK, Lock.LOCK_STATUS_UNLOCK_REQUESTED);
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
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }

    /// <summary>
    /// 取得目前所有處於假釋觀察期（Probation）之鎖定記錄。
    /// </summary>
    /// <returns>假釋中之鎖定記錄清單。</returns>
    public static List<Lock> GetProbationLocks()
    {
        List<Lock> result = [];
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"select * from Locks where status=@p0 order by LockDate desc";
            using IDataReader rdr = Database.Instance.ExecuteReader(sqlString, Lock.LOCK_STATUS_PROBATION);
            while (rdr.Read())
            {
                result.Add(new Lock
                {
                    Id = Db.DbValueConverter.ToInt64(rdr["LockId"]),
                    IpAddress = Db.DbValueConverter.ToString(rdr["IpAddress"]),
                    LockDate = Db.DbValueConverter.ToDateTime(rdr["LockDate"]),
                    Port = Db.DbValueConverter.ToInt(rdr["Port"]),
                    Status = Db.DbValueConverter.ToInt(rdr["Status"]),
                    TriggerIncident = Db.DbValueConverter.ToInt64(rdr["TriggerIncident"]),
                    UnlockDate = Db.DbValueConverter.ToDateTime(rdr["UnlockDate"])
                });
            }
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }

    /// <summary>
    /// 取得自指定截止時間前建立且處於永久硬封鎖之陳舊記錄（供轉移假釋或歸檔）。
    /// </summary>
    /// <param name="cutoffDate">截止時間點（例如 90 天前）。</param>
    /// <returns>陳舊永久鎖定清單。</returns>
    public static List<Lock> GetStalePermanentLocks(DateTime cutoffDate)
    {
        List<Lock> result = [];
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"select * from Locks where status=@p0 and LockDate<@p1";
            using IDataReader rdr = Database.Instance.ExecuteReader(sqlString, Lock.LOCK_STATUS_HARDLOCK, cutoffDate);
            while (rdr.Read())
            {
                result.Add(new Lock
                {
                    Id = Db.DbValueConverter.ToInt64(rdr["LockId"]),
                    IpAddress = Db.DbValueConverter.ToString(rdr["IpAddress"]),
                    LockDate = Db.DbValueConverter.ToDateTime(rdr["LockDate"]),
                    Port = Db.DbValueConverter.ToInt(rdr["Port"]),
                    Status = Db.DbValueConverter.ToInt(rdr["Status"]),
                    TriggerIncident = Db.DbValueConverter.ToInt64(rdr["TriggerIncident"]),
                    UnlockDate = Db.DbValueConverter.ToDateTime(rdr["UnlockDate"])
                });
            }
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }

    /// <summary>
    /// 檢查指定之 IP 位址目前是否處於假釋觀察期（Probation）。
    /// </summary>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <returns>若處於假釋觀察期則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsProbation(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return false;
        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);

        if (Database.Instance.IsConfigured)
        {
            object? result = Database.Instance.ExecuteScalar(
                "select count(*) from Locks where IpAddress=@p0 and status=@p1",
                ipAddress,
                Lock.LOCK_STATUS_PROBATION);
            return result != null && int.TryParse(result.ToString(), out int count) && count > 0;
        }
        return false;
    }

    /// <summary>
    /// 將指定鎖定記錄之狀態轉移為假釋觀察期（Probation）。
    /// </summary>
    /// <param name="lockId">鎖定記錄識別碼。</param>
    public static void SetProbation(long lockId)
    {
        if (Database.Instance.IsConfigured)
        {
            Database.Instance.ExecuteNonQuery(
                "update Locks set Status=@p0, LastUpdate=@p1 where LockId=@p2",
                Lock.LOCK_STATUS_PROBATION,
                DateTime.UtcNow,
                lockId);
        }
    }

    /// <summary>
    /// 依 IP 位址取得目前處於活躍封鎖狀態之鎖定記錄。
    /// </summary>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <returns>若存在傳回 <see cref="Lock"/> 執行個體；否則傳回 <see langword="null"/>。</returns>
    public static Lock? GetActiveLockByIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return null;
        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);

        if (!Database.Instance.IsConfigured) return null;

        using IDataReader reader = Database.Instance.ExecuteReader(
            "select * from Locks where IpAddress=@p0 and status in (@p1,@p2,@p3,@p4) order by LockId desc limit 1",
            ipAddress,
            Lock.LOCK_STATUS_HARDLOCK,
            Lock.LOCK_STATUS_SOFTLOCK,
            Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
            Lock.LOCK_STATUS_SOFTLOCK_REQUESTED);

        if (reader.Read())
        {
            return new Lock
            {
                Id = Db.DbValueConverter.ToInt64(reader["LockId"]),
                IpAddress = Db.DbValueConverter.ToString(reader["IpAddress"]),
                LockDate = Db.DbValueConverter.ToDateTime(reader["LockDate"]),
                Port = Db.DbValueConverter.ToInt(reader["Port"]),
                Status = Db.DbValueConverter.ToInt(reader["Status"]),
                TriggerIncident = Db.DbValueConverter.ToInt64(reader["TriggerIncident"]),
                UnlockDate = Db.DbValueConverter.ToDateTime(reader["UnlockDate"])
            };
        }
        return null;
    }

    /// <summary>
    /// 將指定 IP 位址之活躍鎖定標記為待解除並更新狀態為已解除。
    /// </summary>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <returns>若成功解除傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool UnlockIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return false;
        ipAddress = IpAddressCanonicalizer.Canonicalize(ipAddress);

        if (!Database.Instance.IsConfigured) return false;

        Database.Instance.ExecuteNonQuery(
            "update Locks set Status=@p0, LastUpdate=@p1 where IpAddress=@p2 and status in (@p3,@p4,@p5,@p6)",
            Lock.LOCK_STATUS_UNLOCKED,
            DateTime.UtcNow,
            ipAddress,
            Lock.LOCK_STATUS_HARDLOCK,
            Lock.LOCK_STATUS_SOFTLOCK,
            Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
            Lock.LOCK_STATUS_SOFTLOCK_REQUESTED);

        return true;
    }
}

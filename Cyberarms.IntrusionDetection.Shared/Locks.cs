using System;
using System.Collections.Generic;
using System.Data;


namespace Cyberarms.IntrusionDetection.Shared;

public class Locks
{



    /// <summary>
    /// Determines whether s updates.
    /// </summary>
    /// <param name="lastUpdate">The last update value.</param>
    /// <returns><see langword="true"/> if s updates; otherwise, <see langword="false"/>.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }



    /// <summary>
    /// Reads locks.
    /// </summary>
    /// <returns>The read locks result.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Executes the today operation.
    /// </summary>
    /// <returns>The today result.</returns>

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
                throw new ApplicationException("Invalid data");
            }
        }
        else
        {
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Reads current soft locks.
    /// </summary>
    /// <returns>The read current soft locks result.</returns>

    public static int ReadCurrentSoftLocks()
    {
        object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where status in (@p0) ", (int)LockStatus.SoftLocked);
        int.TryParse(result?.ToString(), out int softLocks);
        return softLocks;
    }

    /// <summary>
    /// Reads current hard locks.
    /// </summary>
    /// <returns>The read current hard locks result.</returns>

    public static int ReadCurrentHardLocks()
    {
        object? result = Database.Instance.ExecuteScalar("select count(*) from Locks where status in (@p0)", (int)LockStatus.HardLocked);
        int.TryParse(result?.ToString(), out int hardLocks);
        return hardLocks;
    }

    /// <summary>
    /// Reads unsuccessful login attempts.
    /// </summary>
    /// <param name="startDate">The start date value.</param>
    /// <returns>The read unsuccessful login attempts result.</returns>

    public static int ReadUnsuccessfulLoginAttempts(DateTime startDate)
    {
        object? result = Database.Instance.ExecuteScalar("select count(*) from IntrusionLog where IncidentTime>@p0 and Action in (@p1,@p2,@p3)", startDate, IntrusionLog.STATUS_INTRUSION_ATTEMPT, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE);
        int.TryParse(result?.ToString(), out int intrusionAttempts);
        return intrusionAttempts;
    }

    /// <summary>
    /// Gets current locks.
    /// </summary>
    /// <returns>The get current locks result.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Gets lock by id.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <returns>The get lock by id result.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Executes the lock exists operation.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Creates lock.
    /// </summary>
    /// <param name="lockDate">The lock date value.</param>
    /// <param name="unlockDate">The unlock date value.</param>
    /// <param name="triggerIncident">The trigger incident value.</param>
    /// <param name="status">The status value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="ipAddress">The ip address value.</param>
    /// <returns>The create lock result.</returns>

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
    /// Creates lock.
    /// </summary>
    /// <param name="l">The l value.</param>
    /// <returns>The create lock result.</returns>

    public static long CreateLock(Lock l)
    {
        if (Database.Instance.IsConfigured)
        {
            Lock result = new();
            string sqlString = @"insert into Locks(LockDate, UnlockDate, TriggerIncident, Status, Port, IpAddress, LastUpdate) values (@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            Database.Instance.ExecuteNonQuery(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.Now);
            object? id = Database.Instance.ExecuteScalar("SELECT last_insert_rowid()");
            l.Id = Db.DbValueConverter.ToInt64(id);
            return l.Id;
        }
        else
        {
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Updates lock.
    /// </summary>
    /// <param name="l">The l value.</param>

    public static void UpdateLock(Lock l)
    {
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"update Locks set LockDate=@p0, UnlockDate=@p1, TriggerIncident=@p2, Status=@p3, Port=@p4, IpAddress=@p5, LastUpdate=@p6 where LockId=@p7";
            Database.Instance.ExecuteNonQuery(sqlString, l.LockDate, l.UnlockDate, l.TriggerIncident, l.Status, l.Port, l.IpAddress, DateTime.Now, l.Id);
        }
        else
        {
            throw new ApplicationException("Database not initialized");
        }
    }

    /// <summary>
    /// Gets unlock list.
    /// </summary>
    /// <returns>The get unlock list result.</returns>

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
            throw new ApplicationException("Database not initialized");
        }
    }



}

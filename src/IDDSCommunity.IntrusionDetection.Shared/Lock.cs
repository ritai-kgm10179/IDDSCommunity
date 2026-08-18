using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表單一已封鎖來源 IP 規則之實體資料模型。
/// </summary>
public class Lock
{
        /// <summary>
    /// 定義 LOCK_STATUS_NONE 之數值。
    /// </summary>
public const int LOCK_STATUS_NONE = 100;
        /// <summary>
    /// 定義 LOCK_STATUS_SOFTLOCK_REQUESTED 之數值。
    /// </summary>
public const int LOCK_STATUS_SOFTLOCK_REQUESTED = 200;
        /// <summary>
    /// 定義 LOCK_STATUS_SOFTLOCK 之數值。
    /// </summary>
public const int LOCK_STATUS_SOFTLOCK = 210;
        /// <summary>
    /// 定義 LOCK_STATUS_SOFTLOCK_EXPIRED 之數值。
    /// </summary>
public const int LOCK_STATUS_SOFTLOCK_EXPIRED = 220;
        /// <summary>
    /// 定義 LOCK_STATUS_HARDLOCK_REQUESTED 之數值。
    /// </summary>
public const int LOCK_STATUS_HARDLOCK_REQUESTED = 300;
        /// <summary>
    /// 定義 LOCK_STATUS_HARDLOCK 之數值。
    /// </summary>
public const int LOCK_STATUS_HARDLOCK = 310;
        /// <summary>
    /// 定義 LOCK_STATUS_HARDLOCK_EXPIRED 之數值。
    /// </summary>
public const int LOCK_STATUS_HARDLOCK_EXPIRED = 320;
        /// <summary>
    /// 定義 LOCK_STATUS_MANUAL 之數值。
    /// </summary>
public const int LOCK_STATUS_MANUAL = 400;
        /// <summary>
    /// 定義 LOCK_STATUS_ACTIVE 之數值。
    /// </summary>
public const int LOCK_STATUS_ACTIVE = 510;
        /// <summary>
    /// 定義 LOCK_STATUS_UNLOCK_REQUESTED 之數值。
    /// </summary>
public const int LOCK_STATUS_UNLOCK_REQUESTED = 500;
        /// <summary>
    /// 定義 LOCK_STATUS_UNLOCKED 之數值。
    /// </summary>
public const int LOCK_STATUS_UNLOCKED = 510;
        /// <summary>
    /// 定義 LOCK_STATUS_HISTORY 之數值。
    /// </summary>
public const int LOCK_STATUS_HISTORY = 800;
        /// <summary>
    /// 定義 LOCK_STATUS_LOCK_ERROR 之數值。
    /// </summary>
public const int LOCK_STATUS_LOCK_ERROR = 900;
        /// <summary>
    /// 定義 LOCK_STATUS_UNLOCK_ERROR 之數值。
    /// </summary>
public const int LOCK_STATUS_UNLOCK_ERROR = 901;
    // Retains persisted legacy status value 999 without exposing the removed licensing feature.
        /// <summary>
    /// 定義 LOCK_STATUS_PROTECTION_UNAVAILABLE 之數值。
    /// </summary>
public const int LOCK_STATUS_PROTECTION_UNAVAILABLE = 999;



        /// <summary>
    /// 取得或設定 Id。
    /// </summary>
public long Id { get; set; }
        /// <summary>
    /// 取得或設定 IpAddress。
    /// </summary>
public string IpAddress { get; set; } = string.Empty;
        /// <summary>
    /// 取得或設定 LockDate。
    /// </summary>
public DateTime LockDate { get; set; }
        /// <summary>
    /// 取得或設定 UnlockDate。
    /// </summary>
public DateTime UnlockDate { get; set; }
        /// <summary>
    /// 取得或設定 Port。
    /// </summary>
public int Port { get; set; }
        /// <summary>
    /// 取得或設定 Status。
    /// </summary>
public int Status { get; set; }
        /// <summary>
    /// 取得或設定 NumberOfSoftLocks。
    /// </summary>
public int NumberOfSoftLocks { get; set; }
        /// <summary>
    /// 取得或設定 TriggerIncident。
    /// </summary>
public long TriggerIncident { get; set; }
    /// <summary>
    /// 儲存設定變更作業。
    /// </summary>
    public void Save()
    {
        if (Database.Instance.IsConfigured)
        {
            string sqlString = "update Locks set IpAddress=@p0, LockDate=@p1, Port=@p2, Status=@p3, TriggerIncident=@p4, UnlockDate=@p5, LastUpdate=@p6 where LockId=" + Id.ToString();
            Database.Instance.ExecuteNonQuery(sqlString, IpAddress, LockDate, Port, Status, TriggerIncident, UnlockDate, DateTime.UtcNow);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
}

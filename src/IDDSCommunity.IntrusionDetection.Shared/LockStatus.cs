using System;
using System.Collections.Generic;


namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義 IP 封鎖狀態（如軟封鎖、硬封鎖、手動解除）列舉。
/// </summary>
public enum LockStatus
{
        /// <summary>
    /// 定義 None 列舉值。
    /// </summary>
None = Lock.LOCK_STATUS_NONE,
        /// <summary>
    /// 定義 SoftLockRequested 列舉值。
    /// </summary>
SoftLockRequested = Lock.LOCK_STATUS_SOFTLOCK_REQUESTED,
        /// <summary>
    /// 處於軟封鎖狀態。
    /// </summary>
SoftLocked = Lock.LOCK_STATUS_SOFTLOCK,
        /// <summary>
    /// 定義 SoftLockExpired 列舉值。
    /// </summary>
SoftLockExpired = Lock.LOCK_STATUS_SOFTLOCK_EXPIRED,
        /// <summary>
    /// 定義 HardLockRequested 列舉值。
    /// </summary>
HardLockRequested = Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
        /// <summary>
    /// 處於硬封鎖狀態。
    /// </summary>
HardLocked = Lock.LOCK_STATUS_HARDLOCK,
        /// <summary>
    /// 定義 HardLockExpired 列舉值。
    /// </summary>
HardLockExpired = Lock.LOCK_STATUS_HARDLOCK_EXPIRED,
        /// <summary>
    /// 處於未封鎖或已解除狀態。
    /// </summary>
Unlocked = Lock.LOCK_STATUS_UNLOCKED,
        /// <summary>
    /// 定義 ManuallyUnlocked 列舉值。
    /// </summary>
ManuallyUnlocked = Lock.LOCK_STATUS_MANUAL,
        /// <summary>
    /// 定義 LockError 列舉值。
    /// </summary>
LockError = Lock.LOCK_STATUS_LOCK_ERROR,
        /// <summary>
    /// 定義 UnlockError 列舉值。
    /// </summary>
UnlockError = Lock.LOCK_STATUS_UNLOCK_ERROR,
        /// <summary>
    /// 定義 ProtectionUnavailable 列舉值。
    /// </summary>
ProtectionUnavailable = Lock.LOCK_STATUS_PROTECTION_UNAVAILABLE
}

/// <summary>
/// 提供封鎖狀態列舉與資料庫數值之間轉換之適配器類別。
/// </summary>
public class LockStatusAdapter
{
    private static Dictionary<int, string>? _lockStatusNames;
        /// <summary>
    /// 取得或設定 LockStatusNames。
    /// </summary>
public static Dictionary<int, string> LockStatusNames
    {
        get
        {
            _lockStatusNames ??= new Dictionary<int, string>
                {
                    { (int)LockStatus.None, "New" },
                    { (int)LockStatus.SoftLockRequested, "Soft lock requested" },
                    { (int)LockStatus.SoftLocked, "Soft lock" },
                    { (int)LockStatus.SoftLockExpired, "Soft lock expired" },
                    { (int)LockStatus.HardLockRequested, "Hard lock requested" },
                    { (int)LockStatus.HardLocked, "Hard lock" },
                    { (int)LockStatus.HardLockExpired, "Hard lock expired" },
                    { (int)LockStatus.Unlocked, "Unlocked" },
                    { (int)LockStatus.ManuallyUnlocked, "Manually unlocked" },
                    { (int)LockStatus.LockError, "Error adding lock" },
                    { (int)LockStatus.UnlockError, "Unlock error" },
                    { (int)LockStatus.ProtectionUnavailable, "Protection unavailable" }
                };
            return _lockStatusNames;
        }
    }
    /// <summary>
    /// 取得鎖定狀態名稱。
    /// </summary>
    /// <param name="status">status參數。</param>
    /// <returns>傳回get lock status name結果。</returns>
    public static string GetLockStatusName(int status)
    {
        if (LockStatusNames.TryGetValue(status, out string? value))
        {
            return Localization.Strings.Get(value);
        }
        else
        {
            return Localization.Strings.Format("Status {0} not found in LockStatusNames!", status);
        }
    }
}

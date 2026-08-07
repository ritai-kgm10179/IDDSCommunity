using System;
using System.Collections.Generic;


namespace IDDSCommunity.IntrusionDetection.Shared;

public enum LockStatus
{
    None = Lock.LOCK_STATUS_NONE,
    SoftLockRequested = Lock.LOCK_STATUS_SOFTLOCK_REQUESTED,
    SoftLocked = Lock.LOCK_STATUS_SOFTLOCK,
    SoftLockExpired = Lock.LOCK_STATUS_SOFTLOCK_EXPIRED,
    HardLockRequested = Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
    HardLocked = Lock.LOCK_STATUS_HARDLOCK,
    HardLockExpired = Lock.LOCK_STATUS_HARDLOCK_EXPIRED,
    Unlocked = Lock.LOCK_STATUS_UNLOCKED,
    ManuallyUnlocked = Lock.LOCK_STATUS_MANUAL,
    LockError = Lock.LOCK_STATUS_LOCK_ERROR,
    UnlockError = Lock.LOCK_STATUS_UNLOCK_ERROR,
    ProtectionUnavailable = Lock.LOCK_STATUS_PROTECTION_UNAVAILABLE
}

public class LockStatusAdapter
{
    private static Dictionary<int, string>? _lockStatusNames;
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

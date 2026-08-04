using System;
using System.Collections.Generic;


namespace Cyberarms.IntrusionDetection.Shared;

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
    LicenseRequired = Lock.LOCK_STATUS_LICENSE_REQUIRED
}

public class LockStatusAdapter
{
    private static Dictionary<int, string> _lockStatusNames;
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
                    { (int)LockStatus.LicenseRequired, "License limitation" }
                };
            return _lockStatusNames;
        }
    }

    public static string GetLockStatusName(int status)
    {
        if (LockStatusNames.ContainsKey(status))
        {
            return LockStatusNames[status];
        }
        else
        {
            return string.Format("Status {0} not found in LockStatusNames!", status);
        }
    }
}

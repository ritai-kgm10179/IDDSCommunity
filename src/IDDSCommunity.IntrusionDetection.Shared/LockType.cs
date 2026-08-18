namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義 IP 封鎖類型（軟封鎖或硬封鎖）列舉。
/// </summary>
public enum LockType
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
    /// 軟封鎖模式。
    /// </summary>
SoftLock = Lock.LOCK_STATUS_SOFTLOCK,
        /// <summary>
    /// 定義 HardLockRequested 列舉值。
    /// </summary>
HardLockRequested = Lock.LOCK_STATUS_HARDLOCK_REQUESTED,
        /// <summary>
    /// 硬封鎖模式。
    /// </summary>
HardLock = Lock.LOCK_STATUS_HARDLOCK
}

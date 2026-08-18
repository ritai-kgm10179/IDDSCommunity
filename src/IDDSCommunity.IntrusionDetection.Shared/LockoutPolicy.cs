using System;

namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// Calculates bounded source-IP lockout delays for repeated authentication failures.
/// </summary>
public static class LockoutPolicy
{
        /// <summary>
    /// 定義 MaximumSoftLockMinutes 之數值。
    /// </summary>
public const int MaximumSoftLockMinutes = 60;
    /// <summary>
    /// Doubles the base delay for each recent lock and caps the result to prevent overflow and excessive denial of service.
    /// </summary>
    public static int CalculateSoftLockMinutes(int baseMinutes, int priorLockCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(priorLockCount);

        int delay = Math.Min(baseMinutes, MaximumSoftLockMinutes);
        for (int index = 0; index < priorLockCount && delay < MaximumSoftLockMinutes; index++)
        {
            delay = Math.Min(delay * 2, MaximumSoftLockMinutes);
        }
        return delay;
    }
}

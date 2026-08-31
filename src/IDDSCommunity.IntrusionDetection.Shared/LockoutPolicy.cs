using System;

namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// 提供來源 IP 於重複驗證失敗時之漸進式鎖定時長計算原則。
/// </summary>
public static class LockoutPolicy
{
    /// <summary>
    /// 定義預設軟鎖定最大時長上限（30 天，換算為 43,200 分鐘）。
    /// </summary>
    public const int DefaultMaxSoftLockMinutes = 43200;

    /// <summary>
    /// 定義舊型 MaximumSoftLockMinutes 相容常數。
    /// </summary>
    public const int MaximumSoftLockMinutes = 60;

    /// <summary>
    /// 定義預設重複違規自動升級硬封鎖（永久封鎖）之軟封鎖次數門檻（5 次）。
    /// </summary>
    public const int DefaultAutoHardLockThreshold = 5;

    /// <summary>
    /// 依據基礎鎖定分鐘數、近期被鎖定次數以及上限值，以指數翻倍演算法計算漸進式軟鎖定分鐘數。
    /// </summary>
    /// <param name="baseMinutes">基礎鎖定分鐘數；必須大於或等於 1。</param>
    /// <param name="priorLockCount">近期被鎖定次數；不可為負數。</param>
    /// <param name="maxMinutes">允許之最大鎖定分鐘數；預設為 30 天。</param>
    /// <returns>傳回計算後之軟鎖定分鐘數。</returns>
    /// <exception cref="ArgumentOutOfRangeException">當 <paramref name="baseMinutes"/> 小於 1 或 <paramref name="priorLockCount"/> 為負數時擲出。</exception>
    public static int CalculateSoftLockMinutes(int baseMinutes, int priorLockCount, int maxMinutes = DefaultMaxSoftLockMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(priorLockCount);

        int effectiveMax = Math.Max(baseMinutes, Math.Max(1, maxMinutes));
        long delay = baseMinutes;
        for (int index = 0; index < priorLockCount && delay < effectiveMax; index++)
        {
            delay = Math.Min(delay * 2, (long)effectiveMax);
        }
        return (int)Math.Min(delay, effectiveMax);
    }

    /// <summary>
    /// 判斷來源 IP 之近期被軟封鎖次數是否已達到自動升級為硬封鎖（永久封鎖）之門檻。
    /// </summary>
    /// <param name="priorLockCount">近期被鎖定次數。</param>
    /// <param name="threshold">升級門檻次數；若未指定則使用預設值 5 次。</param>
    /// <returns>若已達到或超過升級門檻傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool ShouldEscalateToHardLock(int priorLockCount, int threshold = DefaultAutoHardLockThreshold) =>
        priorLockCount >= Math.Max(1, threshold);
}

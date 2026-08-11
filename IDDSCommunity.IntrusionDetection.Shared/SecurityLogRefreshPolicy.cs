using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供安全性記錄滾動視窗的確定性刷新判斷。
/// </summary>
public static class SecurityLogRefreshPolicy
{
    /// <summary>
    /// 判斷是否應重新載入完整安全性記錄視窗。
    /// </summary>
    /// <param name="currentTime">目前時間。</param>
    /// <param name="lastRefreshTime">上次完整刷新時間。</param>
    /// <param name="lastObservedLogId">上次已觀察到的最新事件識別碼。</param>
    /// <param name="currentLogId">資料庫目前最新事件識別碼。</param>
    /// <param name="refreshInterval">沒有新事件時的最長刷新間隔。</param>
    /// <returns>若應重新載入則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool ShouldRefresh(
        DateTime currentTime,
        DateTime lastRefreshTime,
        int lastObservedLogId,
        int currentLogId,
        TimeSpan refreshInterval)
    {
        if (refreshInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(refreshInterval), "刷新間隔必須大於零。");

        return currentLogId > lastObservedLogId ||
            currentTime < lastRefreshTime ||
            currentTime - lastRefreshTime >= refreshInterval;
    }
}

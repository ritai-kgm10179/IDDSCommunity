namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 表示 Windows 防火牆非同步調步佇列與批次聚合器之組態設定。
/// </summary>
public sealed record FirewallPacingOptions
{
    /// <summary>
    /// 取得或設定批次聚合的最大項目數量（預設為 500 筆，不超過單條規則上限 1,000 筆）。
    /// </summary>
    public int MaxBatchSize { get; init; } = 500;

    /// <summary>
    /// 取得或設定聚合等待時間範圍（毫秒），在此時間範圍內到達之請求將被合流聚合（預設為 50 毫秒）。
    /// </summary>
    public int CoalescingWindowMs { get; init; } = 50;

    /// <summary>
    /// 取得或設定兩次 COM 底層寫入操作之間的最小間隔時間（毫秒），避免頻繁突發 COM 呼叫導致 Windows 防火牆服務癱瘓（預設為 25 毫秒）。
    /// </summary>
    public int MinPacingIntervalMs { get; init; } = 25;

    /// <summary>
    /// 取得或設定調步佇列的最大容量上限（預設為 100,000 筆），避免記憶體無限制膨脹。
    /// </summary>
    public int MaxQueueCapacity { get; init; } = 100_000;

    /// <summary>
    /// 取得或設定同步 Block 與 Remove 呼叫是否等待 COM 批次寫入完成（預設為 false，即快速入列非同步提交）。
    /// </summary>
    public bool WaitForCommitOnSync { get; init; }

    /// <summary>
    /// 取得預設組態設定執行個體。
    /// </summary>
    public static FirewallPacingOptions Default { get; } = new();
}

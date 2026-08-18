namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// 提供標準化安全性觀察事件之確定性冪等去重篩選器，防止同一來源事件重複投遞或重播。
/// </summary>
public sealed class ObservationIdempotencyFilter
{
    private readonly int maxCapacity;
    private readonly TimeSpan retentionPeriod;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, long> seenKeys;
    private long totalAccepted;
    private long totalDuplicates;

    /// <summary>
    /// 初始化 <see cref="ObservationIdempotencyFilter"/> 類別的新執行個體。
    /// </summary>
    /// <param name="maxCapacity">最大容納之去重鍵值容量上限。</param>
    /// <param name="retentionPeriod">去重鍵值保留時間（TTL）。</param>
    /// <param name="timeProvider">時間提供者執行個體，預設為系統時間。</param>
    public ObservationIdempotencyFilter(
        int maxCapacity = 50000,
        TimeSpan? retentionPeriod = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCapacity);
        this.maxCapacity = maxCapacity;
        this.retentionPeriod = retentionPeriod ?? TimeSpan.FromMinutes(30);
        this.timeProvider = timeProvider ?? TimeProvider.System;
        seenKeys = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
    }

    /// <summary>
    /// 取得目前已記錄之去重鍵值數量。
    /// </summary>
    public int ActiveKeyCount => seenKeys.Count;

    /// <summary>
    /// 取得歷來成功接受的新事件總數。
    /// </summary>
    public long TotalAccepted => Interlocked.Read(ref totalAccepted);

    /// <summary>
    /// 取得歷來成功阻絕之重複事件總數。
    /// </summary>
    public long TotalDuplicates => Interlocked.Read(ref totalDuplicates);

    /// <summary>
    /// 評估並嘗試接受傳入之安全性觀察事件。若此事件先前已處理過則判定為重複並傳回 <see langword="false"/>。
    /// </summary>
    /// <param name="observation">欲評估之標準化安全性觀察事件。</param>
    /// <param name="idempotencyKey">傳出計算所得之確定性冪等鍵值。</param>
    /// <returns>若為新事件且成功接受傳回 <see langword="true"/>；若為重播或重複事件則傳回 <see langword="false"/>。</returns>
    public bool TryAccept(SecurityObservationEvent observation, out string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(observation);
        idempotencyKey = observation.ComputeIdempotencyKey();

        long nowTicks = timeProvider.GetUtcNow().UtcTicks;
        long expireTicks = nowTicks + retentionPeriod.Ticks;

        // 若容量超限，執行清理過期項目
        if (seenKeys.Count >= maxCapacity)
        {
            PruneExpired(nowTicks);
        }

        // 原子性嘗試加入
        if (seenKeys.TryAdd(idempotencyKey, expireTicks))
        {
            Interlocked.Increment(ref totalAccepted);
            return true;
        }

        // 鍵值已存在：檢查是否已過期
        if (seenKeys.TryGetValue(idempotencyKey, out long existingExpireTicks))
        {
            if (nowTicks > existingExpireTicks)
            {
                // 已過期，更新為新的過期時間並接受
                seenKeys[idempotencyKey] = expireTicks;
                Interlocked.Increment(ref totalAccepted);
                return true;
            }
        }

        // 仍在保留期限內，判定為重複重播
        Interlocked.Increment(ref totalDuplicates);
        return false;
    }

    /// <summary>
    /// 清除快取中所有已過期之冪等鍵值。
    /// </summary>
    /// <param name="currentUtcTicks">目前 UTC 時間刻度。</param>
    public void PruneExpired(long? currentUtcTicks = null)
    {
        long now = currentUtcTicks ?? timeProvider.GetUtcNow().UtcTicks;
        foreach (KeyValuePair<string, long> pair in seenKeys)
        {
            if (now > pair.Value)
            {
                seenKeys.TryRemove(pair.Key, out _);
            }
        }
    }

    /// <summary>
    /// 清空去重快取內容。
    /// </summary>
    public void Reset()
    {
        seenKeys.Clear();
        Interlocked.Exchange(ref totalAccepted, 0);
        Interlocked.Exchange(ref totalDuplicates, 0);
    }
}

namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// 提供具備有界容量、執行緒安全與滑動時間窗過期機制之事件聚合貯列資料結構。
/// </summary>
/// <typeparam name="TKey">聚合鍵之型別。</typeparam>
/// <typeparam name="TItem">貯列中儲存之項目型別。</typeparam>
public sealed class ConcurrentSlidingBucket<TKey, TItem>
    where TKey : notnull
{
    private readonly int maxKeyCapacity;
    private readonly int maxItemsPerBucket;
    private readonly TimeSpan windowDuration;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<TKey, BucketState> buckets;
    private readonly object capacitySync = new();
    private long totalItemsAdded;
    private long totalEvictions;

    /// <summary>
    /// 初始化 <see cref="ConcurrentSlidingBucket{TKey, TItem}"/> 類別的新執行個體。
    /// </summary>
    /// <param name="maxKeyCapacity">最大允許之聚合鍵數量上限。</param>
    /// <param name="maxItemsPerBucket">單一貯列中最大保留項目數上限。</param>
    /// <param name="windowDuration">滑動時間窗有效持續時間。</param>
    /// <param name="timeProvider">時間提供者執行個體，預設為系統時間。</param>
    public ConcurrentSlidingBucket(
        int maxKeyCapacity = 10000,
        int maxItemsPerBucket = 1000,
        TimeSpan? windowDuration = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxKeyCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxItemsPerBucket);
        this.maxKeyCapacity = maxKeyCapacity;
        this.maxItemsPerBucket = maxItemsPerBucket;
        this.windowDuration = windowDuration ?? TimeSpan.FromMinutes(10);
        this.timeProvider = timeProvider ?? TimeProvider.System;
        buckets = new ConcurrentDictionary<TKey, BucketState>();
    }

    /// <summary>
    /// 取得目前活動中的聚合鍵數量。
    /// </summary>
    public int ActiveKeyCount => buckets.Count;

    /// <summary>
    /// 取得累計已加入的項目總數。
    /// </summary>
    public long TotalItemsAdded => Interlocked.Read(ref totalItemsAdded);

    /// <summary>
    /// 取得因容量或過期驅逐的項目總數。
    /// </summary>
    public long TotalEvictions => Interlocked.Read(ref totalEvictions);

    /// <summary>
    /// 向指定聚合鍵之貯列中原子性地加入一個項目，並自動修剪過期項目。
    /// </summary>
    /// <param name="key">聚合鍵識別值。</param>
    /// <param name="item">欲加入之項目執行個體。</param>
    /// <param name="timestampUtc">項目發生之時間戳記，若未提供則採用目前時間。</param>
    /// <returns>傳回加入後該貯列在當前時間窗內的有效項目數量。</returns>
    public int Add(TKey key, TItem item, DateTimeOffset? timestampUtc = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        DateTimeOffset now = timestampUtc ?? timeProvider.GetUtcNow();
        DateTimeOffset cutoff = now - windowDuration;
        while (true)
        {
            if (!buckets.TryGetValue(key, out BucketState? state))
            {
                lock (capacitySync)
                {
                    if (!buckets.TryGetValue(key, out state))
                    {
                        PruneExpiredKeys(cutoff);
                        while (buckets.Count >= maxKeyCapacity)
                        {
                            EvictLeastRecentlyUsedKey();
                        }

                        state = buckets.GetOrAdd(key, new BucketState());
                    }
                }
            }

            lock (state.SyncRoot)
            {
                if (!buckets.TryGetValue(key, out BucketState? current) || !ReferenceEquals(state, current))
                {
                    continue;
                }

                PruneExpiredItems(state, cutoff);

                if (state.Items.Count >= maxItemsPerBucket)
                {
                    int oldestIndex = FindOldestItemIndex(state.Items);
                    state.Items.RemoveAt(oldestIndex);
                    Interlocked.Increment(ref totalEvictions);
                }

                state.Items.Add(new TimedItem(item, now));
                if (now > state.LastActivityUtc)
                {
                    state.LastActivityUtc = now;
                }
                Interlocked.Increment(ref totalItemsAdded);
                return state.Items.Count;
            }
        }
    }

    /// <summary>
    /// 取得指定聚合鍵在當前時間窗內的所有有效項目清單。
    /// </summary>
    /// <param name="key">聚合鍵識別值。</param>
    /// <returns>傳回目前有效項目的唯讀清單；若無此鍵則傳回空清單。</returns>
    public IReadOnlyList<TItem> GetActiveItems(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        while (buckets.TryGetValue(key, out BucketState? state))
        {
            lock (state.SyncRoot)
            {
                if (!buckets.TryGetValue(key, out BucketState? current) || !ReferenceEquals(state, current))
                {
                    continue;
                }

                DateTimeOffset cutoff = timeProvider.GetUtcNow() - windowDuration;
                PruneExpiredItems(state, cutoff);

                return state.Items.Select(static x => x.Item).ToList();
            }
        }

        return Array.Empty<TItem>();
    }

    /// <summary>
    /// 取得指定聚合鍵在當前時間窗內的有效項目數量。
    /// </summary>
    /// <param name="key">聚合鍵識別值。</param>
    /// <returns>傳回有效項目計數。</returns>
    public int GetCount(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        while (buckets.TryGetValue(key, out BucketState? state))
        {
            lock (state.SyncRoot)
            {
                if (!buckets.TryGetValue(key, out BucketState? current) || !ReferenceEquals(state, current))
                {
                    continue;
                }

                DateTimeOffset cutoff = timeProvider.GetUtcNow() - windowDuration;
                PruneExpiredItems(state, cutoff);

                return state.Items.Count;
            }
        }

        return 0;
    }

    /// <summary>
    /// 取得目前所有活動中聚合鍵及其有效項目計數之快照字典。
    /// </summary>
    /// <returns>傳回聚合鍵與項目數量的字典快照。</returns>
    public IReadOnlyDictionary<TKey, int> SnapshotCounts()
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow() - windowDuration;
        Dictionary<TKey, int> result = new();
        lock (capacitySync)
        {
            foreach (KeyValuePair<TKey, BucketState> pair in buckets)
            {
                lock (pair.Value.SyncRoot)
                {
                    if (!buckets.TryGetValue(pair.Key, out BucketState? current) || !ReferenceEquals(pair.Value, current))
                    {
                        continue;
                    }

                    PruneExpiredItems(pair.Value, cutoff);

                    if (pair.Value.Items.Count > 0)
                    {
                        result[pair.Key] = pair.Value.Items.Count;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 清除所有超過指定截止時間之聚合鍵。
    /// </summary>
    /// <param name="cutoff">過期截止時間。</param>
    public void PruneExpiredKeys(DateTimeOffset? cutoff = null)
    {
        DateTimeOffset effectiveCutoff = cutoff ?? (timeProvider.GetUtcNow() - windowDuration);
        lock (capacitySync)
        {
            foreach (KeyValuePair<TKey, BucketState> pair in buckets.ToArray())
            {
                lock (pair.Value.SyncRoot)
                {
                    if (!buckets.TryGetValue(pair.Key, out BucketState? current) || !ReferenceEquals(pair.Value, current))
                    {
                        continue;
                    }

                    PruneExpiredItems(pair.Value, effectiveCutoff);

                    if (pair.Value.Items.Count == 0 && pair.Value.LastActivityUtc < effectiveCutoff)
                    {
                        buckets.TryRemove(pair);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 清空所有貯列與聚合狀態。
    /// </summary>
    public void Reset()
    {
        lock (capacitySync)
        {
            buckets.Clear();
            Interlocked.Exchange(ref totalItemsAdded, 0);
            Interlocked.Exchange(ref totalEvictions, 0);
        }
    }

    private void EvictLeastRecentlyUsedKey()
    {
        KeyValuePair<TKey, BucketState>? victim = null;
        DateTimeOffset oldestActivity = DateTimeOffset.MaxValue;
        foreach (KeyValuePair<TKey, BucketState> pair in buckets)
        {
            lock (pair.Value.SyncRoot)
            {
                if (buckets.TryGetValue(pair.Key, out BucketState? current)
                    && ReferenceEquals(pair.Value, current)
                    && pair.Value.LastActivityUtc < oldestActivity)
                {
                    victim = pair;
                    oldestActivity = pair.Value.LastActivityUtc;
                }
            }
        }

        if (!victim.HasValue)
        {
            return;
        }

        lock (victim.Value.Value.SyncRoot)
        {
            if (buckets.TryRemove(victim.Value))
            {
                Interlocked.Add(ref totalEvictions, victim.Value.Value.Items.Count);
            }
        }
    }

    private void PruneExpiredItems(BucketState state, DateTimeOffset cutoff)
    {
        int removed = state.Items.RemoveAll(entry => entry.TimestampUtc < cutoff);
        if (removed > 0)
        {
            Interlocked.Add(ref totalEvictions, removed);
        }
    }

    private static int FindOldestItemIndex(List<TimedItem> items)
    {
        int oldestIndex = 0;
        for (int index = 1; index < items.Count; index++)
        {
            if (items[index].TimestampUtc < items[oldestIndex].TimestampUtc)
            {
                oldestIndex = index;
            }
        }

        return oldestIndex;
    }

    private sealed class BucketState
    {
                /// <summary>
        /// 取得此貯列狀態的同步根物件。
        /// </summary>
public object SyncRoot { get; } = new();
                /// <summary>
        /// 取得或設定 Items。
        /// </summary>
public List<TimedItem> Items { get; } = new();
                /// <summary>
        /// 取得或設定 LastActivityUtc。
        /// </summary>
public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    private readonly struct TimedItem(TItem item, DateTimeOffset timestampUtc)
    {
                /// <summary>
        /// 取得或設定 Item。
        /// </summary>
public TItem Item { get; } = item;
                /// <summary>
        /// 取得或設定 TimestampUtc。
        /// </summary>
public DateTimeOffset TimestampUtc { get; } = timestampUtc;
    }
}

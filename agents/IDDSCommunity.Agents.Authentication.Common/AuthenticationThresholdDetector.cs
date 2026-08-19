using System;
using System.Collections.Generic;
using System.Net;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 依每一來源 IP 的滑動時間窗、事件去重與容量上限，判斷驗證失敗事件是否已達攻擊門檻。
/// </summary>
public sealed class AuthenticationThresholdDetector
{
    private readonly AuthenticationAgentConfiguration configuration;
    private readonly TimeProvider timeProvider;
    private readonly Dictionary<IPAddress, SourceState> sources = [];
    private readonly LinkedList<IPAddress> recency = [];
    private readonly object sync = new();

    /// <summary>
    /// 初始化 <see cref="AuthenticationThresholdDetector"/> 類別的新執行個體。
    /// </summary>
    /// <param name="configuration">滑動時間窗、門檻值與追蹤容量等設定。</param>
    public AuthenticationThresholdDetector(AuthenticationAgentConfiguration configuration) : this(configuration, TimeProvider.System) { }

    /// <summary>
    /// 以自訂時間來源初始化 <see cref="AuthenticationThresholdDetector"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="configuration">滑動時間窗、門檻值與追蹤容量等設定。</param>
    /// <param name="timeProvider">用於取得目前時間之提供者。</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> 或 <paramref name="timeProvider"/> 為 <see langword="null"/>。</exception>
    internal AuthenticationThresholdDetector(AuthenticationAgentConfiguration configuration, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(timeProvider);
        configuration.Validate();
        this.configuration = configuration;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// 取得目前正在追蹤之來源位址數量。
    /// </summary>
    internal int TrackedSourceCount { get { lock (sync) return sources.Count; } }

    /// <summary>
    /// 分析單筆驗證失敗事件，並判斷其所屬來源是否已達攻擊門檻。
    /// </summary>
    /// <param name="failure">待分析之驗證失敗事件。</param>
    /// <returns>已達攻擊門檻時傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool Analyze(AuthenticationFailureEvent failure)
    {
        lock (sync) return AnalyzeCore(failure);
    }

    private bool AnalyzeCore(AuthenticationFailureEvent failure)
    {
        IPAddress source = Normalize(failure.SourceAddress);
        DateTimeOffset observedAt = timeProvider.GetUtcNow();
        DateTimeOffset occurredAt = failure.OccurredAt > observedAt.AddMinutes(5) ? observedAt : failure.OccurredAt;
        RemoveExpiredSources(observedAt);
        if (!sources.TryGetValue(source, out SourceState? state))
        {
            if (sources.Count >= configuration.MaximumTrackedSources)
                RemoveOldestSource();
            LinkedListNode<IPAddress> node = recency.AddLast(source);
            state = new SourceState(node, observedAt);
            sources[source] = state;
        }
        else
        {
            state.LastObservedAt = observedAt;
            recency.Remove(state.RecencyNode);
            recency.AddLast(state.RecencyNode);
        }
        DateTimeOffset cutoff = occurredAt.AddSeconds(-configuration.WindowSeconds);
        // 以完整篩選（而非只檢查佇列前端）移除逾期時間戳記，因為跨多個輪詢檔案來源時
        // 事件抵達順序不保證單調遞增，僅檢查前端可能永久遺漏埋在佇列中段的逾期項目。
        state.Timestamps.RemoveAll(timestamp => timestamp < cutoff);
        state.Identities.RemoveWhere(item => item.OccurredAt < cutoff);
        FailureIdentity identity = new(occurredAt, failure.EventId, failure.Category, failure.AccountName);
        if (!state.Identities.Add(identity)) return false;
        state.Timestamps.Add(occurredAt);
        if (state.Timestamps.Count < configuration.FailureThreshold)
            return false;
        state.Timestamps.Clear();
        state.Identities.Clear();
        return true;
    }

    private void RemoveExpiredSources(DateTimeOffset now)
    {
        DateTimeOffset cutoff = now.AddSeconds(-configuration.SourceStateRetentionSeconds);
        while (recency.First is not null && sources[recency.First.Value].LastObservedAt <= cutoff)
            RemoveSource(recency.First.Value);
    }

    /// <summary>
    /// 於追蹤容量已滿時淘汰一個來源以騰出空間。在最舊的少數幾個候選來源中，優先淘汰目前於
    /// 時間窗內尚無失敗紀錄者，避免攻擊者以大量偽來源灌爆容量藉此擠掉即將達到門檻的真實攻擊來源；
    /// 若最舊的候選皆有進行中的失敗紀錄，仍會淘汰真正最舊者以保證容量上限確實受控。
    /// </summary>
    private void RemoveOldestSource()
    {
        const int protectedScanLimit = 8;
        LinkedListNode<IPAddress>? candidate = recency.First;
        LinkedListNode<IPAddress>? oldest = candidate;
        int scanned = 0;
        while (candidate is not null && scanned < protectedScanLimit)
        {
            if (sources[candidate.Value].Timestamps.Count == 0)
            {
                RemoveSource(candidate.Value);
                return;
            }
            candidate = candidate.Next;
            scanned++;
        }
        if (oldest is not null) RemoveSource(oldest.Value);
    }

    private void RemoveSource(IPAddress source)
    {
        SourceState state = sources[source];
        recency.Remove(state.RecencyNode);
        sources.Remove(source);
    }

    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private sealed class SourceState(LinkedListNode<IPAddress> recencyNode, DateTimeOffset lastObservedAt)
    {
        internal List<DateTimeOffset> Timestamps { get; } = [];
        internal HashSet<FailureIdentity> Identities { get; } = [];
        internal LinkedListNode<IPAddress> RecencyNode { get; } = recencyNode;
        internal DateTimeOffset LastObservedAt { get; set; } = lastObservedAt;
    }

    private readonly record struct FailureIdentity(DateTimeOffset OccurredAt, int EventId, string Category, string AccountName);
}

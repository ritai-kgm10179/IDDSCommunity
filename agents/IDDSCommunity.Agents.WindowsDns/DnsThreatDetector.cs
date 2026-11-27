using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace IDDSCommunity.Agents.WindowsDns;

internal sealed class DnsThreatDetector(WindowsDnsConfiguration configuration, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<IPAddress, ClientWindow> clients = new();
    private int trackedCount;

    /// <summary>
    /// 取得目前記憶體中保留的用戶端視窗上限數量。
    /// </summary>
    internal int TrackedClientCount => Volatile.Read(ref trackedCount);

    /// <summary>
    /// 評估一項支援的 DNS 活動事件，僅於超越門檻值時引發通知。
    /// </summary>
    /// <param name="record">標準化 DNS 事件。</param>
    /// <returns>傳回偵測到的威脅；若未超越門檻值則傳回 <see langword="null"/>。</returns>
    internal DnsDetection? Analyze(DnsEventRecord record)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        long nowTicks = now.UtcTicks;
        if (!clients.TryGetValue(record.SourceAddress, out ClientWindow? window))
        {
            if (Volatile.Read(ref trackedCount) >= configuration.MaximumTrackedClients)
            {
                EnsureCapacity(nowTicks);
            }

            ClientWindow created = new(now, nowTicks);
            if (clients.TryAdd(record.SourceAddress, created))
            {
                Interlocked.Increment(ref trackedCount);
                window = created;
            }
            else if (!clients.TryGetValue(record.SourceAddress, out window))
            {
                window = clients.GetOrAdd(record.SourceAddress, static (_, state) => state, created);
                if (ReferenceEquals(window, created))
                {
                    Interlocked.Increment(ref trackedCount);
                }
            }
        }

        lock (window)
        {
            if (now - window.StartedAt >= TimeSpan.FromSeconds(configuration.WindowSeconds))
                window.Reset(now, nowTicks);
            Volatile.Write(ref window.LastSeenTicks, nowTicks);
            switch (record.Kind)
            {
                case DnsActivityKind.Query:
                    window.QueryCount++;
                    if (record.IsNxDomain)
                        window.NxDomainCount++;
                    if (record.IsAnyQuery)
                        window.AnyQueryCount++;
                    if (window.AnyQueryCount == configuration.AnyQueryThreshold)
                        return new DnsDetection(DnsDetectionType.AnyQueryRate, record);
                    if (window.NxDomainCount == configuration.NxDomainThreshold)
                        return new DnsDetection(DnsDetectionType.NxDomainRate, record);
                    if (window.QueryCount == configuration.QueryRateThreshold)
                        return new DnsDetection(DnsDetectionType.QueryRate, record);
                    break;
                case DnsActivityKind.DynamicUpdate:
                    window.DynamicUpdateCount++;
                    if (window.DynamicUpdateCount == configuration.DynamicUpdateThreshold)
                        return new DnsDetection(DnsDetectionType.DynamicUpdateRate, record);
                    break;
                case DnsActivityKind.ZoneTransfer:
                    window.ZoneTransferCount++;
                    if (window.ZoneTransferCount == configuration.ZoneTransferThreshold)
                        return new DnsDetection(DnsDetectionType.ZoneTransfer, record);
                    break;
            }
        }
        return null;
    }

    private void EnsureCapacity(long nowTicks)
    {
        if (Volatile.Read(ref trackedCount) < configuration.MaximumTrackedClients)
            return;

        long staleBoundaryTicks = nowTicks - TimeSpan.FromSeconds(configuration.WindowSeconds * 2L).Ticks;
        IPAddress? oldestKey = null;
        long oldestTicks = long.MaxValue;
        int scanned = 0;

        foreach (KeyValuePair<IPAddress, ClientWindow> pair in clients)
        {
            long lastSeenTicks = Volatile.Read(ref pair.Value.LastSeenTicks);
            if (lastSeenTicks < staleBoundaryTicks)
            {
                if (clients.TryRemove(pair.Key, out _))
                {
                    Interlocked.Decrement(ref trackedCount);
                }
                continue;
            }

            if (lastSeenTicks < oldestTicks)
            {
                oldestTicks = lastSeenTicks;
                oldestKey = pair.Key;
            }

            if (++scanned >= 256 && Volatile.Read(ref trackedCount) < configuration.MaximumTrackedClients)
            {
                return;
            }
        }

        if (Volatile.Read(ref trackedCount) < configuration.MaximumTrackedClients)
            return;

        if (oldestKey is not null && clients.TryRemove(oldestKey, out _))
        {
            Interlocked.Decrement(ref trackedCount);
        }
    }

    private sealed class ClientWindow(DateTimeOffset startedAt, long nowTicks)
    {
        internal long LastSeenTicks = nowTicks;
        internal DateTimeOffset StartedAt { get; private set; } = startedAt;
        internal int QueryCount { get; set; }
        internal int NxDomainCount { get; set; }
        internal int AnyQueryCount { get; set; }
        internal int DynamicUpdateCount { get; set; }
        internal int ZoneTransferCount { get; set; }

        internal void Reset(DateTimeOffset now, long ticks)
        {
            StartedAt = now;
            Volatile.Write(ref LastSeenTicks, ticks);
            QueryCount = 0;
            NxDomainCount = 0;
            AnyQueryCount = 0;
            DynamicUpdateCount = 0;
            ZoneTransferCount = 0;
        }
    }
}

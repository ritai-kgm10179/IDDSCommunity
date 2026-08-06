using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IDDSCommunity.Agents.WindowsDns;

internal sealed class DnsThreatDetector(WindowsDnsConfiguration configuration, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<IPAddress, ClientWindow> clients = new();

    /// <summary>
    /// 取得目前記憶體中保留的用戶端視窗上限數量。
    /// </summary>
    internal int TrackedClientCount => clients.Count;

    /// <summary>
    /// 評估一項支援的 DNS 活動事件，僅於超越門檻值時引發通知。
    /// </summary>
    /// <param name="record">標準化 DNS 事件。</param>
    /// <returns>傳回偵測到的威脅；若未超越門檻值則傳回 <see langword="null"/>。</returns>
    internal DnsDetection? Analyze(DnsEventRecord record)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        EnsureCapacity(now);
        ClientWindow window = clients.GetOrAdd(record.SourceAddress, _ => new ClientWindow(now));
        lock (window)
        {
            if (now - window.StartedAt >= TimeSpan.FromSeconds(configuration.WindowSeconds))
                window.Reset(now);
            window.LastSeenAt = now;
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

    private void EnsureCapacity(DateTimeOffset now)
    {
        if (clients.Count < configuration.MaximumTrackedClients)
            return;
        DateTimeOffset staleBoundary = now - TimeSpan.FromSeconds(configuration.WindowSeconds * 2L);
        foreach (KeyValuePair<IPAddress, ClientWindow> pair in clients)
        {
            if (pair.Value.LastSeenAt < staleBoundary)
                clients.TryRemove(pair.Key, out _);
        }
        if (clients.Count < configuration.MaximumTrackedClients)
            return;
        KeyValuePair<IPAddress, ClientWindow> oldest = clients.OrderBy(pair => pair.Value.LastSeenAt).First();
        clients.TryRemove(oldest.Key, out _);
    }

    private sealed class ClientWindow(DateTimeOffset startedAt)
    {
        internal DateTimeOffset StartedAt { get; private set; } = startedAt;
        internal DateTimeOffset LastSeenAt { get; set; } = startedAt;
        internal int QueryCount { get; set; }
        internal int NxDomainCount { get; set; }
        internal int AnyQueryCount { get; set; }
        internal int DynamicUpdateCount { get; set; }
        internal int ZoneTransferCount { get; set; }

        internal void Reset(DateTimeOffset now)
        {
            StartedAt = now;
            LastSeenAt = now;
            QueryCount = 0;
            NxDomainCount = 0;
            AnyQueryCount = 0;
            DynamicUpdateCount = 0;
            ZoneTransferCount = 0;
        }
    }
}

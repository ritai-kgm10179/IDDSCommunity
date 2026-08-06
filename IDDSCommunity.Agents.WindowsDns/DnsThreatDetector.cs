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
    /// Gets the current bounded number of client windows retained in memory.
    /// </summary>
    internal int TrackedClientCount => clients.Count;

    /// <summary>
    /// Evaluates one supported DNS activity event and emits only when a threshold is crossed.
    /// </summary>
    /// <param name="record">The normalized DNS event.</param>
    /// <returns>The detected threat, or <see langword="null"/> when no threshold was crossed.</returns>
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

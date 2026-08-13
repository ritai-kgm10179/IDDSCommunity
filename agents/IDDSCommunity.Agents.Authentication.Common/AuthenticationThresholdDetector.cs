using System;
using System.Collections.Generic;
using System.Net;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class AuthenticationThresholdDetector
{
    private readonly AuthenticationAgentConfiguration configuration;
    private readonly TimeProvider timeProvider;
    private readonly Dictionary<IPAddress, SourceState> sources = [];
    private readonly LinkedList<IPAddress> recency = [];
    private readonly object sync = new();

    public AuthenticationThresholdDetector(AuthenticationAgentConfiguration configuration) : this(configuration, TimeProvider.System) { }

    internal AuthenticationThresholdDetector(AuthenticationAgentConfiguration configuration, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(timeProvider);
        configuration.Validate();
        this.configuration = configuration;
        this.timeProvider = timeProvider;
    }

    internal int TrackedSourceCount { get { lock (sync) return sources.Count; } }

    public bool Analyze(AuthenticationFailureEvent failure)
    {
        lock (sync) return AnalyzeCore(failure);
    }

    private bool AnalyzeCore(AuthenticationFailureEvent failure)
    {
        IPAddress source = Normalize(failure.SourceAddress);
        DateTimeOffset observedAt = timeProvider.GetUtcNow();
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
        DateTimeOffset cutoff = failure.OccurredAt.AddSeconds(-configuration.WindowSeconds);
        while (state.Timestamps.Count > 0 && state.Timestamps.Peek() < cutoff)
            state.Timestamps.Dequeue();
        state.Identities.RemoveWhere(item => item.OccurredAt < cutoff);
        FailureIdentity identity = new(failure.OccurredAt, failure.EventId, failure.Category, failure.AccountName);
        if (!state.Identities.Add(identity)) return false;
        state.Timestamps.Enqueue(failure.OccurredAt);
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

    private void RemoveOldestSource()
    {
        if (recency.First is not null) RemoveSource(recency.First.Value);
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
        internal Queue<DateTimeOffset> Timestamps { get; } = [];
        internal HashSet<FailureIdentity> Identities { get; } = [];
        internal LinkedListNode<IPAddress> RecencyNode { get; } = recencyNode;
        internal DateTimeOffset LastObservedAt { get; set; } = lastObservedAt;
    }

    private readonly record struct FailureIdentity(DateTimeOffset OccurredAt, int EventId, string Category, string AccountName);
}

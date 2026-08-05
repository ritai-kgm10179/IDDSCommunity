using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class AuthenticationThresholdDetector
{
    private readonly AuthenticationAgentConfiguration configuration;
    private readonly HashSet<IPAddress> excluded;
    private readonly Dictionary<IPAddress, Queue<DateTimeOffset>> failures = [];
    private readonly Dictionary<IPAddress, HashSet<FailureIdentity>> identities = [];
    private readonly object sync = new();

    public AuthenticationThresholdDetector(AuthenticationAgentConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        this.configuration = configuration;
        excluded = configuration.ExcludedAddresses.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => IPAddress.TryParse(value, out IPAddress? address) ? address : null)
            .Where(address => address is not null)
            .Cast<IPAddress>()
            .ToHashSet();
    }

    public bool Analyze(AuthenticationFailureEvent failure)
    {
        lock (sync) return AnalyzeCore(failure);
    }

    private bool AnalyzeCore(AuthenticationFailureEvent failure)
    {
        IPAddress source = Normalize(failure.SourceAddress);
        if (IPAddress.IsLoopback(source) || excluded.Contains(source))
            return false;
        if (!failures.TryGetValue(source, out Queue<DateTimeOffset>? timestamps))
        {
            if (failures.Count >= configuration.MaximumTrackedSources)
                RemoveOldestSource();
            timestamps = new Queue<DateTimeOffset>();
            failures[source] = timestamps;
            identities[source] = [];
        }
        DateTimeOffset cutoff = failure.OccurredAt.AddSeconds(-configuration.WindowSeconds);
        while (timestamps.Count > 0 && timestamps.Peek() < cutoff)
            timestamps.Dequeue();
        HashSet<FailureIdentity> sourceIdentities = identities[source];
        sourceIdentities.RemoveWhere(item => item.OccurredAt < cutoff);
        FailureIdentity identity = new(failure.OccurredAt, failure.EventId, failure.Category, failure.AccountName);
        if (!sourceIdentities.Add(identity)) return false;
        timestamps.Enqueue(failure.OccurredAt);
        if (timestamps.Count < configuration.FailureThreshold)
            return false;
        timestamps.Clear();
        sourceIdentities.Clear();
        return true;
    }

    private void RemoveOldestSource()
    {
        IPAddress? oldest = failures.Where(item => item.Value.Count > 0).OrderBy(item => item.Value.Peek()).Select(item => item.Key).FirstOrDefault();
        if (oldest is not null)
        {
            failures.Remove(oldest);
            identities.Remove(oldest);
        }
        else if (failures.Count > 0)
        {
            IPAddress first = failures.Keys.First();
            failures.Remove(first);
            identities.Remove(first);
        }
    }

    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    private readonly record struct FailureIdentity(DateTimeOffset OccurredAt, int EventId, string Category, string AccountName);
}

using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Authentication.Common;

public class AuthenticationAgentConfiguration : PluginConfiguration
{
    public int WindowSeconds { get; set; } = 300;
    public int FailureThreshold { get; set; } = 10;
    public int MaximumTrackedSources { get; set; } = 10000;
    public string ExcludedAddresses { get; set; } = "127.0.0.1;::1";

    public virtual void Validate()
    {
        if (WindowSeconds is < 10 or > 86400)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Detection window must be between 10 and 86400 seconds."));
        if (FailureThreshold is < 2 or > 100000)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Failure threshold must be between 2 and 100000."));
        if (MaximumTrackedSources is < 100 or > 1000000)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Tracked source capacity must be between 100 and 1000000."));
    }
}

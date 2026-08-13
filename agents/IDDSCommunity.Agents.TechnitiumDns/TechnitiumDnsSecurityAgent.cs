using System;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.TechnitiumDns;

[SupportedOSPlatform("windows7.0")]
[Plugin("Technitium DNS Security Agent", "Detects blocked or abusive Technitium DNS queries.", "1.0")]
public sealed class TechnitiumDnsSecurityAgent : AuthenticationAgentBase<TechnitiumDnsConfiguration>
{
    public TechnitiumDnsSecurityAgent() : this(new TechnitiumDnsConfiguration()) { }
    private TechnitiumDnsSecurityAgent(TechnitiumDnsConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    internal TechnitiumDnsSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Technitium DNS Security Agent"); set { } }
    public override Guid Id => new("{99C71D22-830E-4E76-A83B-7D831C2442FE}");

    private static IAuthenticationEventSource CreateSource(TechnitiumDnsConfiguration configuration)
    {
        return new PollingLogFileFailureSource(
            configuration.EnumerateLogFiles,
            line => TechnitiumDnsLogParser.TryParseMessage(line, DateTimeOffset.UtcNow));
    }
}

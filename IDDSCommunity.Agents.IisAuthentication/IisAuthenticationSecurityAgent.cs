using System;
using System.Drawing;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.IisAuthentication;

[Plugin("IIS Authentication Security Agent", "Detects repeated IIS authentication failures, including optional OWA paths.", "1.0")]
public sealed class IisAuthenticationSecurityAgent : AuthenticationAgentBase<IisAuthenticationConfiguration>
{
    public IisAuthenticationSecurityAgent() : this(new IisAuthenticationConfiguration()) { }
    private IisAuthenticationSecurityAgent(IisAuthenticationConfiguration configuration) : base(new PollingLogFileFailureSource(configuration.EnumerateLogFiles, new IisW3cAuthenticationParser(configuration.GetProtectedPaths()).Parse)) => Configuration.AgentSettings = configuration;
    internal IisAuthenticationSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    protected override Color AgentColor => Color.FromArgb(48, 138, 158);
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("IIS Authentication Security Agent"); set { } }
    public override Guid Id => new("{6B87C539-1585-41E5-A12F-D5073EF6D631}");
}

using System;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.FileZilla;

[SupportedOSPlatform("windows7.0")]
[Plugin("FileZilla Security Agent", "Detects repeated FileZilla Server authentication failures.", "1.0")]
public sealed class FileZillaSecurityAgent : AuthenticationAgentBase<FileZillaConfiguration>
{
    public FileZillaSecurityAgent() : this(new FileZillaConfiguration()) { }
    private FileZillaSecurityAgent(FileZillaConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    internal FileZillaSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("FileZilla Security Agent"); set { } }
    public override Guid Id => new("{88B67B54-9E7D-4F7B-8A5F-4E90B0F33A11}");

    private static IAuthenticationEventSource CreateSource(FileZillaConfiguration configuration)
    {
        return new PollingLogFileFailureSource(
            configuration.EnumerateLogFiles,
            line => FileZillaLogParser.TryParseMessage(line, DateTimeOffset.UtcNow));
    }
}

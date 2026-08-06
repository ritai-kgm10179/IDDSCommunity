using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.OpenSsh;

public sealed class OpenSshConfiguration : AuthenticationAgentConfiguration
{
    public bool ReadEventLog { get; set; } = true;
    public string LogFilePath { get; set; } = string.Empty;
    internal IEnumerable<string> EnumerateLogFiles() => File.Exists(LogFilePath) ? [LogFilePath] : [];
    public override void Validate()
    {
        base.Validate();
        if (!ReadEventLog && string.IsNullOrWhiteSpace(LogFilePath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("OpenSSH requires Windows event log reading or a log file."));
        if (!string.IsNullOrWhiteSpace(LogFilePath) && !Path.IsPathFullyQualified(LogFilePath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("OpenSSH log file must be an absolute path."));
    }
}

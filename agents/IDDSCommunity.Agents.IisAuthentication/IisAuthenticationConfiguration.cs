using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.IisAuthentication;

public sealed class IisAuthenticationConfiguration : AuthenticationAgentConfiguration
{
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "inetpub", "logs", "LogFiles");
    public string ProtectedPaths { get; set; } = string.Empty;
    internal string[] GetProtectedPaths() => ProtectedPaths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    internal IEnumerable<string> EnumerateLogFiles() => Directory.Exists(LogDirectory) ? Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.AllDirectories) : [];
    public override void Validate() { base.Validate(); if (!Path.IsPathFullyQualified(LogDirectory)) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("IIS log directory must be an absolute path.")); }
}

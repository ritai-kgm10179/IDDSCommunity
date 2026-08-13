using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla;

[SupportedOSPlatform("windows7.0")]
public sealed class FileZillaConfiguration : AuthenticationAgentConfiguration
{
    public string LogDirectoryPath { get; set; } = @"C:\ProgramData\filezilla-server\logs\";
    public string LogFilePattern { get; set; } = "*.log";

    public IEnumerable<string> EnumerateLogFiles()
    {
        string directoryPath = string.IsNullOrWhiteSpace(LogDirectoryPath) ? @"C:\ProgramData\filezilla-server\logs\" : LogDirectoryPath;
        string pattern = string.IsNullOrWhiteSpace(LogFilePattern) ? "*.log" : LogFilePattern;

        if (!Directory.Exists(directoryPath)) return Array.Empty<string>();
        return Directory.EnumerateFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
    }

    public override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(LogDirectoryPath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("FileZilla log directory path must be specified."));
    }
}

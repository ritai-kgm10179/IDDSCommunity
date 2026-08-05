using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.PostgreSql;

public sealed class PostgreSqlConfiguration : AuthenticationAgentConfiguration
{
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PostgreSQL");
    public string SearchPattern { get; set; } = "*.log";

    public override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(LogDirectory) || !Path.IsPathFullyQualified(LogDirectory)) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL log directory must be an absolute path."));
        if (string.IsNullOrWhiteSpace(SearchPattern) || SearchPattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL log pattern is invalid."));
    }

    internal IEnumerable<string> EnumerateLogFiles() => Directory.Exists(LogDirectory) ? Directory.EnumerateFiles(LogDirectory, SearchPattern, SearchOption.AllDirectories) : [];
}

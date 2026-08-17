using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.PostgreSql;

/// <summary>
/// 提供 PostgreSQL Security Agent 監看伺服器記錄檔目錄之相關設定。
/// </summary>
public sealed class PostgreSqlConfiguration : AuthenticationAgentConfiguration
{
    /// <summary>
    /// 取得或設定 PostgreSQL 記錄檔所在目錄。
    /// </summary>
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PostgreSQL");
    /// <summary>
    /// 取得或設定用於篩選記錄檔案名稱的萬用字元樣式。
    /// </summary>
    public string SearchPattern { get; set; } = "*.log";

    /// <summary>
    /// 驗證設定值是否有效。
    /// </summary>
    /// <exception cref="InvalidOperationException">記錄檔目錄或篩選樣式無效。</exception>
    public override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(LogDirectory) || !Path.IsPathFullyQualified(LogDirectory)) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL log directory must be an absolute path."));
        if (string.IsNullOrWhiteSpace(SearchPattern) || SearchPattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL log pattern is invalid."));
    }

    internal IEnumerable<string> EnumerateLogFiles() => Directory.Exists(LogDirectory) ? Directory.EnumerateFiles(LogDirectory, SearchPattern, SearchOption.AllDirectories) : [];
}

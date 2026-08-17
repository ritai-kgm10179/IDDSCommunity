using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla;

/// <summary>
/// 提供 FileZilla Security Agent 監看 FileZilla Server 驗證記錄檔目錄之相關設定。
/// </summary>
[SupportedOSPlatform("windows7.0")]
public sealed class FileZillaConfiguration : AuthenticationAgentConfiguration
{
    /// <summary>
    /// 取得或設定 FileZilla Server 記錄檔所在目錄。
    /// </summary>
    public string LogDirectoryPath { get; set; } = @"C:\ProgramData\filezilla-server\logs\";
    /// <summary>
    /// 取得或設定用於篩選記錄檔案名稱的萬用字元樣式。
    /// </summary>
    public string LogFilePattern { get; set; } = "*.log";

    /// <summary>
    /// 列舉目前設定目錄下符合檔名樣式的所有記錄檔。
    /// </summary>
    /// <returns>符合條件之記錄檔完整路徑集合；目錄不存在時傳回空集合。</returns>
    public IEnumerable<string> EnumerateLogFiles()
    {
        string directoryPath = string.IsNullOrWhiteSpace(LogDirectoryPath) ? @"C:\ProgramData\filezilla-server\logs\" : LogDirectoryPath;
        string pattern = string.IsNullOrWhiteSpace(LogFilePattern) ? "*.log" : LogFilePattern;

        if (!Directory.Exists(directoryPath)) return Array.Empty<string>();
        return Directory.EnumerateFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
    }

    /// <summary>
    /// 驗證設定值是否有效。
    /// </summary>
    /// <exception cref="InvalidOperationException">記錄檔目錄路徑未指定。</exception>
    public override void Validate()
    {
        base.Validate();
        if (string.IsNullOrWhiteSpace(LogDirectoryPath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("FileZilla log directory path must be specified."));
    }
}

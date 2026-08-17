using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.IisAuthentication;

/// <summary>
/// 提供 IIS Authentication Security Agent 監看 W3C 記錄檔目錄與受保護路徑之相關設定。
/// </summary>
public sealed class IisAuthenticationConfiguration : AuthenticationAgentConfiguration
{
    /// <summary>
    /// 取得或設定 IIS W3C 記錄檔所在目錄。
    /// </summary>
    public string LogDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3), "inetpub", "logs", "LogFiles");
    /// <summary>
    /// 取得或設定以分號分隔之受保護路徑前綴清單；為空字串時不限制路徑。
    /// </summary>
    public string ProtectedPaths { get; set; } = string.Empty;
    internal string[] GetProtectedPaths() => ProtectedPaths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    internal IEnumerable<string> EnumerateLogFiles() => Directory.Exists(LogDirectory) ? Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.AllDirectories) : [];
    /// <summary>
    /// 驗證設定值是否有效。
    /// </summary>
    /// <exception cref="InvalidOperationException">記錄檔目錄非絕對路徑。</exception>
    public override void Validate() { base.Validate(); if (!Path.IsPathFullyQualified(LogDirectory)) throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("IIS log directory must be an absolute path.")); }
}

using System;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.OpenSsh;

/// <summary>
/// 提供 Windows OpenSSH Security Agent 之事件記錄與文字記錄檔來源設定。
/// </summary>
public sealed class OpenSshConfiguration : AuthenticationAgentConfiguration
{
    /// <summary>
    /// 取得或設定是否讀取 <c>OpenSSH/Operational</c> Windows 事件記錄頻道。
    /// </summary>
    public bool ReadEventLog { get; set; } = true;
    /// <summary>
    /// 取得或設定選用之文字記錄檔絕對路徑；為空字串時不啟用文字記錄檔來源。
    /// </summary>
    public string LogFilePath { get; set; } = string.Empty;
    internal IEnumerable<string> EnumerateLogFiles() => File.Exists(LogFilePath) ? [LogFilePath] : [];
    /// <summary>
    /// 驗證設定值是否有效，並確保至少啟用一種事件來源。
    /// </summary>
    /// <exception cref="InvalidOperationException">未啟用任何事件來源，或文字記錄檔路徑非絕對路徑。</exception>
    public override void Validate()
    {
        base.Validate();
        if (!ReadEventLog && string.IsNullOrWhiteSpace(LogFilePath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("OpenSSH requires Windows event log reading or a log file."));
        if (!string.IsNullOrWhiteSpace(LogFilePath) && !Path.IsPathFullyQualified(LogFilePath))
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("OpenSSH log file must be an absolute path."));
    }
}

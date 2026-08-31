using System;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared.Notifications;

/// <summary>
/// 提供將 IDDS Community 入侵與封鎖事件格式化為 RFC 5424、RFC 3164 與 ArcSight CEF 格式之建構工具。
/// </summary>
public static class SyslogPayloadBuilder
{
    private const int FacilitySecurity = 4; // auth / security
    private const int SeverityNotice = 5;
    private const int SeverityWarning = 4;
    private const int SeverityAlert = 1;

    /// <summary>
    /// 建構 Syslog 訊息字串。
    /// </summary>
    /// <param name="format">Syslog 訊息格式。</param>
    /// <param name="lockType">封鎖類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">代理程式名稱。</param>
    /// <param name="details">詳細說明。</param>
    /// <param name="timestampUtc">事件時間 (UTC)。</param>
    /// <param name="hostname">本機主機名稱。</param>
    /// <returns>格式化後之 Syslog 字串。</returns>
    public static string BuildMessage(
        SyslogFormat format,
        LockType lockType,
        string ipAddress,
        string agentName,
        string details,
        DateTime timestampUtc,
        string? hostname = null)
    {
        hostname ??= Environment.MachineName;
        int severity = lockType switch
        {
            LockType.HardLock => SeverityAlert,
            LockType.SoftLock => SeverityWarning,
            _ => SeverityNotice
        };

        int pri = (FacilitySecurity * 8) + severity;

        return format switch
        {
            SyslogFormat.Rfc5424 => BuildRfc5424(pri, timestampUtc, hostname, lockType, ipAddress, agentName, details),
            SyslogFormat.Rfc3164 => BuildRfc3164(pri, timestampUtc, hostname, lockType, ipAddress, agentName, details),
            SyslogFormat.Cef => BuildCef(pri, timestampUtc, hostname, lockType, ipAddress, agentName, details),
            _ => BuildRfc5424(pri, timestampUtc, hostname, lockType, ipAddress, agentName, details)
        };
    }

    private static string BuildRfc5424(int pri, DateTime time, string hostname, LockType lockType, string ip, string agent, string details)
    {
        string timestamp = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string eventAction = lockType.ToString();
        string cleanDetails = details.Replace("\"", "\\\"");

        // <PRI>1 TIMESTAMP HOSTNAME APP-NAME PROCID MSGID [STRUCTURED-DATA] MSG
        return $"<{pri}>1 {timestamp} {hostname} IDDSCommunity {Environment.ProcessId} {eventAction} [intrusion@41123 srcIp=\"{ip}\" agent=\"{agent}\" action=\"{eventAction}\"] {cleanDetails}";
    }

    private static string BuildRfc3164(int pri, DateTime time, string hostname, LockType lockType, string ip, string agent, string details)
    {
        // <PRI>Mmm dd hh:mm:ss HOSTNAME TAG: MSG
        string timestamp = time.ToString("MMM dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        return $"<{pri}>{timestamp} {hostname} IDDSCommunity[{Environment.ProcessId}]: [{agent}] {lockType} for IP {ip} - {details}";
    }

    private static string BuildCef(int pri, DateTime time, string hostname, LockType lockType, string ip, string agent, string details)
    {
        // CEF:Version|Device Vendor|Device Product|Device Version|Device Event Class ID|Name|Severity|[Extension]
        int cefSeverity = lockType switch
        {
            LockType.HardLock => 8,
            LockType.SoftLock => 5,
            _ => 2
        };

        long rt = new DateTimeOffset(time).ToUnixTimeMilliseconds();
        string cleanDetails = details.Replace("|", "\\|").Replace("=", "\\=");

        return $"CEF:0|IDDSCommunity|IntrusionDetection|1.0|{lockType}|{lockType} Applied|{cefSeverity}|src={ip} cs1Label=Agent cs1={agent} msg={cleanDetails} rt={rt} dhost={hostname}";
    }
}

using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.PostgreSql;

/// <summary>
/// 監看 PostgreSQL 伺服器記錄檔（一般文字或 <c>jsonlog</c>），偵測重複的密碼驗證失敗事件。
/// </summary>
[Plugin("PostgreSQL Security Agent", "Detects repeated PostgreSQL password authentication failures from server logs.", "1.0")]
public sealed partial class PostgreSqlSecurityAgent : AuthenticationAgentBase<PostgreSqlConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="PostgreSqlSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public PostgreSqlSecurityAgent() : this(new PostgreSqlConfiguration()) { }
    private PostgreSqlSecurityAgent(PostgreSqlConfiguration configuration) : base(new PollingLogFileFailureSource(configuration.EnumerateLogFiles, TryParseLine)) => Configuration.AgentSettings = configuration;
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="PostgreSqlSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal PostgreSqlSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{E4D503EE-33D9-4A79-A2E3-B19597D49D58}");

    /// <summary>
    /// 嘗試將單行記錄文字（一般文字或 <c>jsonlog</c> 格式）解析為驗證失敗事件。
    /// </summary>
    /// <param name="line">待解析之單行記錄文字。</param>
    /// <returns>解析成功時傳回驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    internal static AuthenticationFailureEvent? TryParseLine(string line)
    {
        AuthenticationFailureEvent? structured = TryParseJson(line);
        if (structured is not null) return structured;
        if (!line.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase)) return null;
        Match ip = IpAddressPattern().Match(line);
        Match user = UserPattern().Match(line);
        if (!ip.Success || !IPAddress.TryParse(ip.Groups["ip"].Value.Trim('[', ']'), out IPAddress? address)) return null;
        return new AuthenticationFailureEvent(DateTimeOffset.UtcNow, address, 0, "PostgreSQL", user.Success ? user.Groups["user"].Value : string.Empty, "Password authentication failed");
    }

    private static AuthenticationFailureEvent? TryParseJson(string line)
    {
        if (!line.AsSpan().TrimStart().StartsWith("{")) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            string message = GetString(root, "message");
            if (!message.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase)) return null;
            string source = GetString(root, "remote_host");
            if (!IPAddress.TryParse(source.Trim('[', ']'), out IPAddress? address)) return null;
            string account = GetString(root, "user");
            DateTimeOffset occurredAt = DateTimeOffset.TryParse(GetString(root, "timestamp"), out DateTimeOffset parsed) ? parsed : DateTimeOffset.UtcNow;
            return new AuthenticationFailureEvent(occurredAt, address, 0, "PostgreSQL", account, "Password authentication failed");
        }
        catch (JsonException) { return null; }
    }

    private static string GetString(JsonElement root, string propertyName) => root.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;

    [GeneratedRegex(@"(?:host=|client=|remote=|\[)(?<ip>\[?[0-9A-Fa-f:.]+\]?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex IpAddressPattern();
    [GeneratedRegex("password authentication failed for user [\"'](?<user>[^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex UserPattern();
}

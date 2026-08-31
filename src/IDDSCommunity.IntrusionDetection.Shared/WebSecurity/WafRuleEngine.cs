using System;
using System.Text.RegularExpressions;

namespace IDDSCommunity.IntrusionDetection.Shared.WebSecurity;

/// <summary>
/// 提供應用層輕量級 WAF (Layer 7 WAF-Lite) 攻擊特徵比對與惡意請求檢測引擎。
/// </summary>
public static class WafRuleEngine
{
    private static readonly Regex SqlInjectionRegex = new(
        @"(?i)(\bunion\s+(all\s+)?select\b|\bselect\b.+\bfrom\b.+\bwhere\b|'\s*or\s+['""\w]+\s*=\s*['""\w]+|--|;\s*drop\s+table\b|\bexec\s*\(\s*xp_cmdshell|\bwaitfor\s+delay\b|\bsleep\s*\(\s*\d+\s*\))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex XssRegex = new(
        @"(?i)(<\s*script\b|javascript\s*:|<\s*img\b[^>]*\bonerror\s*=|onload\s*=|document\.cookie|window\.location)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex PathTraversalRegex = new(
        @"(?i)(\.\.[/\\]|\.\.%2f|\.\.%5c|/etc/passwd|/etc/shadow|c:[/\\]windows[/\\]win\.ini|c:[/\\]boot\.ini)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex CriticalRceRegex = new(
        @"(?i)(\$\{jndi:(ldap|rmi|dns|nis|iiop|corba|http)://|class\.module\.classLoader|\b(cmd|powershell|bash|sh)\.exe\b|\bpassthru\s*\(|\bsystem\s*\()",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex SensitiveProbeRegex = new(
        @"(?i)(\.env$|\.git/config$|\.svn/entries$|wp-config\.php$|web\.config$|\.aws/credentials$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    /// <summary>
    /// 檢查傳入之 URL 或請求內容是否命中常見 Web 攻擊特徵。
    /// </summary>
    /// <param name="input">待檢測之 URL、QueryString 或 Request Body 內容。</param>
    /// <param name="matchedThreatCategory">若命中傳回威脅分類名稱；否則傳回 <see langword="null"/>。</param>
    /// <returns>若判定為惡意攻擊請求傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryMatchThreat(string? input, out string? matchedThreatCategory)
    {
        matchedThreatCategory = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        try
        {
            if (CriticalRceRegex.IsMatch(input))
            {
                matchedThreatCategory = "RCE.Log4Shell.Or.Spring4Shell";
                return true;
            }

            if (SqlInjectionRegex.IsMatch(input))
            {
                matchedThreatCategory = "SQL.Injection";
                return true;
            }

            if (PathTraversalRegex.IsMatch(input))
            {
                matchedThreatCategory = "Path.Traversal";
                return true;
            }

            if (XssRegex.IsMatch(input))
            {
                matchedThreatCategory = "Cross.Site.Scripting";
                return true;
            }

            if (SensitiveProbeRegex.IsMatch(input))
            {
                matchedThreatCategory = "Sensitive.File.Probe";
                return true;
            }

            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            // 防範 ReDoS 攻擊，超時視為安全略過
            return false;
        }
    }
}

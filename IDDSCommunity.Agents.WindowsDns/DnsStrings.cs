using System.Globalization;
using System.Resources;

namespace IDDSCommunity.Agents.WindowsDns;

internal static class DnsStrings
{
    private static readonly ResourceManager ResourceManager = new("IDDSCommunity.Agents.WindowsDns.Resources", typeof(DnsStrings).Assembly);

    /// <summary>
    /// 取得 DNS Agent 的在地化字串（包含不變後備機制）。
    /// </summary>
    /// <param name="key">不變資源金鑰與後備數值。</param>
    /// <returns>傳回在地化數值。</returns>
    internal static string Get(string key) => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>
    /// 使用目前 UI 文化特性格式化在地化 DNS Agent 字串。
    /// </summary>
    /// <param name="key">不變資源金鑰與後備格式字串。</param>
    /// <param name="arguments">插入在地化格式的引數。</param>
    /// <returns>傳回格式化後的在地化數值。</returns>
    internal static string Format(string key, params object?[] arguments) => string.Format(CultureInfo.CurrentUICulture, Get(key), arguments);
}

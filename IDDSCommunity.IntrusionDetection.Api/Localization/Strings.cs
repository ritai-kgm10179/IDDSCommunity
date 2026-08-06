using System.Globalization;
using System.Resources;

namespace IDDSCommunity.IntrusionDetection.Api.Localization;

/// <summary>
/// 為無外部相依性之公開 API 與 Agent 實作提供在地化字串。
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager = new("IDDSCommunity.IntrusionDetection.Api.Localization.Strings", typeof(Strings).Assembly);

    /// <summary>
    /// 取得目前 UI 文化特性的在地化字串。
    /// </summary>
    /// <param name="key">不變資源金鑰。</param>
    /// <returns>傳回在地化數值，若不存在則傳回 <paramref name="key"/>。</returns>
    public static string Get(string key) => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}

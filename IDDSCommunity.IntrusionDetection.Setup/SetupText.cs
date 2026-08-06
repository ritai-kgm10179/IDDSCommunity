using System.Globalization;
using System.Resources;

namespace IDDSCommunity.IntrusionDetection.Setup;

/// <summary>
/// 提供安裝程式在地化文字資源處理。
/// </summary>
internal static class SetupText
{
    private static readonly ResourceManager Resources = new("IDDSCommunity.IntrusionDetection.Setup.SetupStrings", typeof(SetupText).Assembly);

    /// <summary>
    /// 取得在地化的安裝程式字串。
    /// </summary>
    /// <param name="name">資源名稱。</param>
    /// <returns>傳回在地化字串數值。</returns>
    internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    /// <summary>
    /// 格式化在地化的安裝程式字串。
    /// </summary>
    /// <param name="name">資源名稱。</param>
    /// <param name="arguments">格式化引數。</param>
    /// <returns>傳回格式化後的在地化字串數值。</returns>
    internal static string Format(string name, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
}

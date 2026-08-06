using System.Drawing;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 提供驗證 Agent 產生預設主題位元圖圖示之處理處理器。
/// </summary>
internal static class AgentIconFactory
{
    /// <summary>
    /// 依據指定強調色彩與選取狀態建立代理程式圖示位元圖。
    /// </summary>
    /// <param name="accent">圖示強調主色。</param>
    /// <param name="selected">是否為選取狀態。</param>
    /// <returns>回傳產生之位元圖物件，若無自訂圖示則回傳 null。</returns>
    internal static Bitmap? Create(Color accent, bool selected) => null;
}

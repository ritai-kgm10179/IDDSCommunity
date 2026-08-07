using System.Reflection;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 擴充元件自訂設定之基底類別。
/// </summary>
public class PluginConfiguration
{
    /// <summary>
    /// 自同型別的另一個 <see cref="PluginConfiguration"/> 執行個體複製屬性值。
    /// </summary>
    /// <param name="source">來源設定物件。</param>
    public void CloneFrom(PluginConfiguration source)
    {
        foreach (PropertyInfo pi in GetType().GetProperties())
        {
            if (pi.CanWrite)
            {
                pi.SetValue(this, pi.GetValue(source, null), null);
            }
        }
    }
}

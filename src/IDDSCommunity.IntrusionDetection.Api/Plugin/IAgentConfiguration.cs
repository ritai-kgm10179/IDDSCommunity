using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 提供入侵偵測系統載入與儲存 Agent 擴充元件設定所需的屬性與方法介面。
/// </summary>
public interface IAgentConfiguration
{
    /// <summary>
    /// 取得或設定組件檔名。
    /// </summary>
    string AssemblyName { get; set; }
    /// <summary>
    /// 取得或設定 Agent 名稱。
    /// </summary>
    string AgentName { get; set; }
    /// <summary>
    /// 取得或設定是否啟用此 Agent。
    /// </summary>
    bool Enabled { get; set; }
    /// <summary>
    /// 取得或設定 Agent 的自訂設定物件。
    /// </summary>
    PluginConfiguration? AgentSettings { get; set; }
    /// <summary>
    /// 取得或設定自訂設定型別名稱。
    /// </summary>
    string ConfigurationSettingsTypeName { get; set; }
    /// <summary>
    /// 取得擴充元件設定之型別。
    /// </summary>
    /// <returns>傳回對應的 <see cref="Type"/> 執行個體；若找不到則傳回 <see langword="null"/>。</returns>
    Type? GetConfigurationType();
    /// <summary>
    /// 取得或設定軟封鎖觸發次數。
    /// </summary>
    int SoftLockAttempts { get; set; }
    /// <summary>
    /// 取得或設定硬封鎖觸發次數。
    /// </summary>
    int HardLockAttempts { get; set; }
    /// <summary>
    /// 取得或設定軟封鎖持續分鐘數。
    /// </summary>
    int SoftLockDurationMins { get; set; }
    /// <summary>
    /// 取得或設定硬封鎖持續小時數。
    /// </summary>
    int HardLockDurationHrs { get; set; }
    /// <summary>
    /// 取得或設定是否永不解鎖攻擊者的 IP 位址。
    /// </summary>
    bool NeverUnlock { get; set; }
    /// <summary>
    /// 取得或設定是否覆寫此 Agent 的全域設定。
    /// </summary>
    bool OverwriteConfiguration { get; set; }
    /// <summary>
    /// 複製指定 Agent 設定物件的屬性值。
    /// </summary>
    /// <param name="source">來源 Agent 設定物件。</param>
    void CloneFrom(IAgentConfiguration source);
}

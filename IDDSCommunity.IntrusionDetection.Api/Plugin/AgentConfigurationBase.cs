using System;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 可作為自訂 Agent 設定基礎之基底類別。
/// 使用此基底類別，入侵偵測系統會自動載入與儲存擴充元件所需的設定值。
/// </summary>
public class AgentConfigurationBase : IAgentConfiguration
{
    /// <summary>
    /// 取得或設定組件名稱。
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Agent 名稱。
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定是否啟用此 Agent。
    /// </summary>
    public bool Enabled { get; set; }

    private PluginConfiguration? _agentSettings;

    /// <summary>
    /// 取得或設定 Agent 的自訂設定物件。
    /// </summary>
    [XmlIgnore]
    public PluginConfiguration? AgentSettings
    {
        get
        {
            if (_agentSettings is null && !string.IsNullOrEmpty(ConfigurationSettingsTypeName))
            {
                Type? configType = GetConfigurationType();
                if (configType is not null)
                {
                    object? o = Activator.CreateInstance(configType);
                    if (o is PluginConfiguration pluginConfig)
                    {
                        _agentSettings = pluginConfig;
                    }
                }
            }
            return _agentSettings;
        }
        set => _agentSettings = value;
    }

    /// <summary>
    /// 取得或設定自訂設定型別名稱。
    /// </summary>
    public string ConfigurationSettingsTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定擴充元件設定的 XML 序列化內容。
    /// </summary>
    public string? PluginConfigurationXml
    {
        get
        {
            if (AgentSettings is null) return null;
            XmlSerializer xs = new(AgentSettings.GetType());
            StringBuilder sb = new();
            using StringWriter sw = new(sb);
            xs.Serialize(sw, AgentSettings);
            return sb.ToString();
        }
        set
        {
            if (AgentSettings is not null && value is not null)
            {
                XmlSerializer xs = new(AgentSettings.GetType());
                using StringReader sr = new(value);
                AgentSettings = (PluginConfiguration)xs.Deserialize(sr)!;
            }
        }
    }

    /// <summary>
    /// 取得或設定硬封鎖持續小時數。
    /// </summary>
    public int HardLockDurationHrs { get; set; }

    /// <summary>
    /// 取得或設定硬封鎖觸發次數。
    /// </summary>
    public int HardLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定軟封鎖持續分鐘數。
    /// </summary>
    public int SoftLockDurationMins { get; set; }

    /// <summary>
    /// 取得或設定軟封鎖觸發次數。
    /// </summary>
    public int SoftLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定是否覆寫此 Agent 的全域設定。
    /// </summary>
    public bool OverwriteConfiguration { get; set; }

    /// <summary>
    /// 取得或設定是否永不解鎖攻擊者的 IP 位址。
    /// </summary>
    public bool NeverUnlock { get; set; }

    /// <summary>
    /// 取得或設定設定檔路徑名稱。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 複製指定 Agent 設定的屬性值。
    /// </summary>
    /// <param name="source">來源 Agent 設定物件。</param>
    public void CloneFrom(IAgentConfiguration source)
    {
        AssemblyName = source.AssemblyName;
        AgentName = source.AgentName;
        ConfigurationSettingsTypeName = source.ConfigurationSettingsTypeName;
        Enabled = source.Enabled;
        HardLockAttempts = source.HardLockAttempts;
        HardLockDurationHrs = source.HardLockDurationHrs;
        SoftLockAttempts = source.SoftLockAttempts;
        SoftLockDurationMins = source.SoftLockDurationMins;
        OverwriteConfiguration = source.OverwriteConfiguration;
        NeverUnlock = source.NeverUnlock;
        if (!string.IsNullOrEmpty(ConfigurationSettingsTypeName) && source.AgentSettings is not null)
        {
            Type? configType = GetConfigurationType();
            if (configType is not null)
            {
                AgentSettings = (PluginConfiguration)Activator.CreateInstance(configType)!;
                AgentSettings.CloneFrom(source.AgentSettings);
            }
        }
    }

    /// <summary>
    /// 取得擴充元件設定之 Type 執行個體。
    /// </summary>
    /// <returns>傳回組件中對應的 <see cref="Type"/> 執行個體；若找不到則傳回 <see langword="null"/>。</returns>
    public Type? GetConfigurationType()
    {
        if (File.Exists(AssemblyName))
        {
            var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(System.IO.Path.GetFullPath(AssemblyName));
            if (assembly is not null)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsPublic && !type.IsAbstract && type.FullName == ConfigurationSettingsTypeName)
                    {
                        return type;
                    }
                }
            }
        }
        return null;
    }
}

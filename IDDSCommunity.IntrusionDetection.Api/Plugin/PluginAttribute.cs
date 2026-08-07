using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 用於擴充元件指定顯示名稱、說明與版本資訊之自訂屬性。
/// 入侵偵測管理工具會讀取此屬性定義之數值並顯示於介面上。
/// </summary>
/// <param name="displayName">於管理軟體中顯示的名稱。</param>
public class PluginAttribute(string displayName) : Attribute
{
    /// <summary>
    /// 初始化 <see cref="PluginAttribute"/> 類別的新執行個體。
    /// </summary>
    /// <param name="displayName">於管理軟體中顯示的名稱。</param>
    /// <param name="description">Agent 擴充元件的簡短說明。</param>
    /// <param name="version">Agent 擴充元件的版本號碼。</param>
    public PluginAttribute(string displayName, string description, string version)
        : this(displayName, description) => Version = version;
    /// <summary>
    /// 初始化 <see cref="PluginAttribute"/> 類別的新執行個體。
    /// </summary>
    /// <param name="displayName">於管理軟體中顯示的名稱。</param>
    /// <param name="description">Agent 擴充元件的簡短說明。</param>
    public PluginAttribute(string displayName, string description)
        : this(displayName) => Description = description;
    /// <summary>
    /// 取得或設定 Agent 的顯示名稱。
    /// </summary>
    public string DisplayName { get; set; } = displayName;
    /// <summary>
    /// 取得或設定 Agent 的簡短說明。
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// 取得或設定 Agent 的版本號碼。
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.TerminalServer;

/// <summary>
/// 提供 TLS/SSL Security Agent 監聽遠端桌面連接埠之相關設定。
/// </summary>
public class TslSslConfig : PluginConfiguration
{
    private int _rdpPort = 0;
    /// <summary>
    /// 取得或設定監聽的遠端桌面 (RDP) 連接埠；設定為 0 時視為預設值 3389。
    /// </summary>
    [System.ComponentModel.DefaultValue(3389)]
    public int RdpPort
    {
        get => _rdpPort == 0 ? 3389 : _rdpPort; set => _rdpPort = value == 0 ? 3389 : value;
    }
}

using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.FtpServer;

/// <summary>
/// 提供通用 FTP Security Agent 監聽連接埠之相關設定。
/// </summary>
public class FtpConfig : PluginConfiguration
{
    private int _ftpPort = 0;
    /// <summary>
    /// 取得或設定監聽的 FTP 連接埠；設定為 0 時視為預設值 21。
    /// </summary>
    [System.ComponentModel.DefaultValue(21)]
    public int FtpPort
    {
        get => _ftpPort == 0 ? 21 : _ftpPort; set => _ftpPort = value == 0 ? 21 : value;
    }

}

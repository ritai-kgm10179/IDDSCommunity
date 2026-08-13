using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 定義 SMTP Agent 的網路設定。
/// </summary>
public sealed class SmtpConfig : PluginConfiguration
{
    private int _smtpPort = 0;
    /// <summary>
    /// 取得或設定 SMTP 連接埠。
    /// </summary>
    [System.ComponentModel.DefaultValue(25)]
    public int SmtpPort
    {
        get => _smtpPort == 0 ? 25 : _smtpPort; set => _smtpPort = value == 0 ? 25 : value;
    }
}

using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 定義 POP3 Agent 的網路設定。
/// </summary>
public sealed class Pop3Config : PluginConfiguration
{

    private int _pop3Port = 0;
    /// <summary>
    /// 取得或設定 POP3 連接埠。
    /// </summary>
    [System.ComponentModel.DefaultValue(110)]
    public int Pop3Port
    {
        get => _pop3Port == 0 ? 110 : _pop3Port; set => _pop3Port = value == 0 ? 110 : value;
    }


}

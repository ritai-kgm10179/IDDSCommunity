using System.ComponentModel;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 定義明文 IMAP 監控的網路設定。
/// </summary>
public sealed class ImapConfig : PluginConfiguration
{
    private int imapPort;

    /// <summary>
    /// 取得或設定明文 IMAP 連接埠。STARTTLS 封包僅在 TLS 開始前進行檢查。
    /// </summary>
    [DefaultValue(143)]
    public int ImapPort
    {
        get => imapPort == 0 ? 143 : imapPort;
        set => imapPort = value == 0 ? 143 : value;
    }
}

using System.ComponentModel;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// Defines the network settings for cleartext IMAP monitoring.
/// </summary>
public sealed class ImapConfig : PluginConfiguration
{
    private int imapPort;

    /// <summary>
    /// Gets or sets the cleartext IMAP port. STARTTLS traffic is inspected only before TLS begins.
    /// </summary>
    [DefaultValue(143)]
    public int ImapPort
    {
        get => imapPort == 0 ? 143 : imapPort;
        set => imapPort = value == 0 ? 143 : value;
    }
}

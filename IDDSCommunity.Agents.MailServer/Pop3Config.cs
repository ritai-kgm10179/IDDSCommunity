using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MailServer;

public class Pop3Config : PluginConfiguration
{

    private int _pop3Port = 0;
    [System.ComponentModel.DefaultValue(110)]
    public int Pop3Port
    {
        get => _pop3Port == 0 ? 110 : _pop3Port; set => _pop3Port = value == 0 ? 110 : value;
    }


}

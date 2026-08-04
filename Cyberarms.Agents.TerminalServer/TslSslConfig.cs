using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.Agents.TerminalServer;

public class TslSslConfig : PluginConfiguration
{
    private int _rdpPort = 0;
    [System.ComponentModel.DefaultValue(3389)]
    public int RdpPort
    {
        get => _rdpPort == 0 ? 3389 : _rdpPort; set => _rdpPort = value == 0 ? 3389 : value;
    }
}

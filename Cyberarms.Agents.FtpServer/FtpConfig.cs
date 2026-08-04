using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.Agents.FtpServer;

public class FtpConfig : PluginConfiguration
{
    private int _ftpPort = 0;
    [System.ComponentModel.DefaultValue(21)]
    public int FtpPort
    {
        get => _ftpPort == 0 ? 21 : _ftpPort; set => _ftpPort = value == 0 ? 21 : value;
    }

}

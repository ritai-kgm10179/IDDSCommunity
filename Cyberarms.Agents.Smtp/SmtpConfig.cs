using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.Agents.Smtp;

public class SmtpConfig : PluginConfiguration
{
    private int _smtpPort = 0;
    [System.ComponentModel.DefaultValue(25)]
    public int SmtpPort
    {
        get => _smtpPort == 0 ? 25 : _smtpPort; set => _smtpPort = value == 0 ? 25 : value;
    }
}

using Cyberarms.IntrusionDetection.Api.Plugin;

namespace DemoAgent;

public class BadAgent : AgentPlugin
{
    public BadAgent()
    {
    }

    protected override void OnStartAgent()
    {
        base.OnStartAgent();
        while (true) ;

    }
}

using Cyberarms.IntrusionDetection.Api.Plugin;

namespace DemoAgent;

public class BadAgent : AgentPlugin
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadAgent"/> class.
    /// </summary>

    public BadAgent()
    {
    }

    /// <summary>
    /// Processes the start agent notification.
    /// </summary>

    protected override void OnStartAgent()
    {
        base.OnStartAgent();
        while (true) ;

    }
}

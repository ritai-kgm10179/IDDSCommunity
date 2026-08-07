using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace DemoAgent;

public class BadAgent : AgentPlugin
{
    /// <summary>
    /// 初始化 <see cref="BadAgent"/> 類別的新執行個體。
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

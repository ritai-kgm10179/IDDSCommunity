using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Service;

internal interface IIntrusionDetectionRuntime
{
    /// <summary>
    /// Starts all intrusion-detection components as one hosted runtime.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of host startup.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops all started intrusion-detection components.
    /// </summary>
    /// <param name="cancellationToken">Signals that graceful shutdown has exceeded its deadline.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    Task StopAsync(CancellationToken cancellationToken);
}

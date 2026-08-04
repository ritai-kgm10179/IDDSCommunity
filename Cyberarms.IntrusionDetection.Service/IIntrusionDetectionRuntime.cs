using System.Threading;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Service;

internal interface IIntrusionDetectionRuntime
{
    /// <summary>
    /// Starts all intrusion-detection components as one hosted runtime.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of host startup.</param>
    /// <returns>A task that completes when startup succeeds.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops all started intrusion-detection components.
    /// </summary>
    /// <param name="cancellationToken">Signals that graceful shutdown has exceeded its deadline.</param>
    /// <returns>A task that completes when shutdown finishes.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}

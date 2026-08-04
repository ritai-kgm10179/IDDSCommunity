using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class PaladinWorker(Service service) : BackgroundService
{
    /// <summary>
    /// Starts the intrusion-detection runtime and waits until service shutdown is requested.
    /// </summary>
    /// <param name="stoppingToken">Signals that the Windows service is stopping.</param>
    /// <returns>A task representing the worker lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        service.StartHostedService();
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    /// <summary>
    /// Stops all agents and releases service-owned resources.
    /// </summary>
    /// <param name="cancellationToken">Signals that graceful shutdown has exceeded its deadline.</param>
    /// <returns>A completed task after the runtime has stopped.</returns>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        service.StopHostedService();
        return base.StopAsync(cancellationToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class PaladinWorker(IIntrusionDetectionRuntime runtime) : BackgroundService
{
    private bool started;

    /// <summary>
    /// Starts the protected runtime before the host reports that startup completed.
    /// </summary>
    /// <param name="cancellationToken">Signals that host startup was cancelled.</param>
    /// <returns>A task representing hosted-service startup.</returns>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await runtime.StartAsync(cancellationToken).ConfigureAwait(false);
        started = true;
        try
        {
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await runtime.StopAsync(CancellationToken.None).ConfigureAwait(false);
            started = false;
            throw;
        }
    }

    /// <summary>
    /// Starts the intrusion-detection runtime and waits until service shutdown is requested.
    /// </summary>
    /// <param name="stoppingToken">Signals that the Windows service is stopping.</param>
    /// <returns>A task representing the worker lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    /// <summary>
    /// Stops all agents and releases service-owned resources.
    /// </summary>
    /// <param name="cancellationToken">Signals that graceful shutdown has exceeded its deadline.</param>
    /// <returns>A completed task after the runtime has stopped.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        if (started)
        {
            await runtime.StopAsync(cancellationToken).ConfigureAwait(false);
            started = false;
        }
    }
}

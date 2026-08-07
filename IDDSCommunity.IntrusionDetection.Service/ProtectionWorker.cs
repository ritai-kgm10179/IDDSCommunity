using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace IDDSCommunity.IntrusionDetection.Service;

internal sealed class ProtectionWorker(IIntrusionDetectionRuntime runtime) : BackgroundService
{
    private bool started;
    /// <summary>
    /// Starts the protected runtime before the host reports that startup completed.
    /// </summary>
    /// <param name="cancellationToken">Signals that host startup was cancelled.</param>
    /// <returns>表示非同步執行的 Task。</returns>
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
    /// <returns>表示非同步執行的 Task。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
    /// <summary>
    /// 停止執行階段服務。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>執行階段服務停止後完成之 Task。</returns>
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

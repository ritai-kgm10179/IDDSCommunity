using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class CyberarmsRuntimeHealthCheck(Database database, ReportScheduler reportScheduler, SecurityAgents securityAgents) : IHealthCheck
{
    /// <summary>
    /// Reports whether required runtime subsystems are configured and active.
    /// </summary>
    /// <param name="context">The health-check context.</param>
    /// <param name="cancellationToken">Cancels the health check.</param>
    /// <returns>The current runtime health result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<string, object> data = new()
        {
            ["database_configured"] = database.IsConfigured,
            ["report_scheduler_running"] = reportScheduler.IsRunning,
            ["loaded_agents"] = securityAgents.LoadedAgents.Count
        };
        if (!database.IsConfigured)
            return Task.FromResult(HealthCheckResult.Unhealthy(Strings.Get("The runtime database is not configured."), data: data));
        if (!reportScheduler.IsRunning)
            return Task.FromResult(HealthCheckResult.Degraded(Strings.Get("The report scheduler is not running."), data: data));
        return Task.FromResult(HealthCheckResult.Healthy(Strings.Get("Cyberarms runtime is operational."), data));
    }
}

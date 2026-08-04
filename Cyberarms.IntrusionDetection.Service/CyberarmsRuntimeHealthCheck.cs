using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class CyberarmsRuntimeHealthCheck(
    Database database,
    ReportScheduler reportScheduler,
    SecurityAgents securityAgents,
    IOptions<ProtectionOptions> protectionOptions) : IHealthCheck
{
    /// <summary>
    /// Reports whether required runtime subsystems are configured and active.
    /// </summary>
    /// <param name="context">The health-check context.</param>
    /// <param name="cancellationToken">Cancels the health check.</param>
    /// <returns>The current runtime health result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<string, object> data = new()
        {
            ["database_configured"] = database.IsConfigured,
            ["report_scheduler_running"] = reportScheduler.IsRunning,
            ["loaded_agents"] = securityAgents.LoadedAgents.Count
        };
        if (!database.IsConfigured)
            return HealthCheckResult.Unhealthy(Strings.Get("The runtime database is not configured."), data: data);
        try
        {
            await database.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM ProtectionAuditLog", cancellationToken: cancellationToken).ConfigureAwait(false);
            long unfinishedEvents = await database.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM ProtectionEventInbox WHERE Status<>2",
                cancellationToken: cancellationToken).ConfigureAwait(false);
            data["audit_store_available"] = true;
            data["unfinished_security_events"] = unfinishedEvents;
            if (unfinishedEvents > protectionOptions.Value.SecurityEventQueueCapacity)
                return HealthCheckResult.Degraded(Strings.Get("The protection event backlog exceeds the configured queue capacity."), data: data);
        }
        catch (System.Exception exception)
        {
            data["audit_store_available"] = false;
            return HealthCheckResult.Unhealthy(Strings.Get("The protection audit store is unavailable."), exception, data);
        }
        if (!reportScheduler.IsRunning)
            return HealthCheckResult.Degraded(Strings.Get("The report scheduler is not running."), data: data);
        return HealthCheckResult.Healthy(Strings.Get("Cyberarms runtime is operational."), data);
    }
}

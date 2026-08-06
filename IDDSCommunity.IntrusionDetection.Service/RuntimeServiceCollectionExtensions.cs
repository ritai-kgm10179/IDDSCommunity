using System;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IDDSCommunity.IntrusionDetection.Service;

internal static class RuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers one isolated set of intrusion-detection runtime dependencies in a host container.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    internal static IServiceCollection AddIDDSCommunityRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IWindowsEventLog, WindowsEventLog>();
        services.TryAddSingleton<IRuntimeLog, WindowsLogManager>();
        services.TryAddSingleton<IFirewallPolicy>(provider => new FirewallPolicyManager(
            provider.GetRequiredService<IRuntimeLog>(),
            provider.GetRequiredService<IddsConfig>().FirewallBlockMode));
        services.AddSingleton<Database>();
        services.AddSingleton<IddsConfig>();
        services.AddSingleton<NotificationSettings>();
        services.AddSingleton<SecurityAgents>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ProtectionAuditTrail>();
        services.AddSingleton(provider => new ReportScheduler(TimeProvider.System, provider.GetRequiredService<NotificationSettings>()));
        services.AddSingleton<Statistics>();
        services.AddSingleton(provider => new Service(
            provider.GetRequiredService<IFirewallPolicy>(),
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>(),
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PluginOptions>>(),
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReportOptions>>(),
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProtectionOptions>>(),
            provider.GetRequiredService<Database>(),
            provider.GetRequiredService<IddsConfig>(),
            provider.GetRequiredService<NotificationSettings>(),
            provider.GetRequiredService<SecurityAgents>(),
            provider.GetRequiredService<ReportScheduler>(),
            provider.GetRequiredService<Statistics>(),
            provider.GetRequiredService<ProtectionAuditTrail>(),
            provider.GetRequiredService<IRuntimeLog>()));
        services.AddSingleton<IIntrusionDetectionRuntime>(provider => provider.GetRequiredService<Service>());
        services.AddHealthChecks().AddCheck<IDDSCommunityRuntimeHealthCheck>("iddscommunity-runtime");
        return services;
    }
}

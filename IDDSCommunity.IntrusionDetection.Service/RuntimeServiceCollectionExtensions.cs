using System;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IDDSCommunity.IntrusionDetection.Service;

internal static class RuntimeServiceCollectionExtensions
{
    /// <summary>
    /// 使用與 Windows SCM 註冊鍵一致的名稱註冊服務存留期。
    /// </summary>
    /// <param name="services">主機服務集合。</param>
    /// <returns>傳回相同的服務集合。</returns>
    internal static IServiceCollection AddIDDSCommunityWindowsService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Configure<WindowsServiceLifetimeOptions>(options => options.ServiceName = Globals.WINDOWS_SERVICE_NAME);
        services.AddWindowsService();
        return services;
    }

    /// <summary>
    /// Registers one isolated set of intrusion-detection runtime dependencies in a host container.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <returns>傳回 same service collection for chaining 的結果。</returns>
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

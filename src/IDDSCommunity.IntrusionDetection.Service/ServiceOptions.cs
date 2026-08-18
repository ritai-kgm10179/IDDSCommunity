using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDDSCommunity.IntrusionDetection.Service;

internal sealed class DatabaseOptions
{
    internal const string SectionName = "Database";

        /// <summary>
    /// 取得或設定 FileName。
    /// </summary>
[Required]
    public string FileName { get; init; } = "iddscommunity.dbf";
}

internal sealed class ReportOptions
{
    internal const string SectionName = "Reports";

        /// <summary>
    /// 取得或設定 CheckIntervalMinutes。
    /// </summary>
[Range(1, 1440)]
    public int CheckIntervalMinutes { get; init; } = 10;
}

internal sealed class PluginOptions
{
    internal const string SectionName = "Plugins";

        /// <summary>
    /// 取得或設定 DirectoryName。
    /// </summary>
[Required]
    public string DirectoryName { get; init; } = "Plugins";
}

internal sealed class ProtectionOptions
{
    internal const string SectionName = "Protection";

        /// <summary>
    /// 取得或設定 AuditRetentionDays。
    /// </summary>
[Range(30, 3650)]
    public int AuditRetentionDays { get; init; } = 365;

        /// <summary>
    /// 取得或設定 IntrusionLogRetentionDays。
    /// </summary>
[Range(30, 3650)]
    public int IntrusionLogRetentionDays { get; init; } = 180;

        /// <summary>
    /// 取得或設定 LockHistoryRetentionDays。
    /// </summary>
[Range(30, 3650)]
    public int LockHistoryRetentionDays { get; init; } = 180;

        /// <summary>
    /// 取得或設定 CompletedEventRetentionDays。
    /// </summary>
[Range(1, 3650)]
    public int CompletedEventRetentionDays { get; init; } = 30;

        /// <summary>
    /// 取得或設定 MaintenanceBatchSize。
    /// </summary>
[Range(1, 10000)]
    public int MaintenanceBatchSize { get; init; } = 1000;

        /// <summary>
    /// 取得或設定 MaintenanceIntervalHours。
    /// </summary>
[Range(1, 168)]
    public int MaintenanceIntervalHours { get; init; } = 24;

        /// <summary>
    /// 取得或設定 BackupRetentionDays。
    /// </summary>
[Range(1, 3650)]
    public int BackupRetentionDays { get; init; } = 30;

        /// <summary>
    /// 取得或設定 MaximumBackupCount。
    /// </summary>
[Range(1, 1000)]
    public int MaximumBackupCount { get; init; } = 10;

        /// <summary>
    /// 取得或設定 AutomaticBackupEnabled。
    /// </summary>
public bool AutomaticBackupEnabled { get; init; } = true;

        /// <summary>
    /// 取得或設定 SecurityEventQueueCapacity。
    /// </summary>
[Range(16, 1048576)]
    public int SecurityEventQueueCapacity { get; init; } = 4096;

        /// <summary>
    /// 取得或設定 SecurityEventDrainTimeoutSeconds。
    /// </summary>
[Range(1, 300)]
    public int SecurityEventDrainTimeoutSeconds { get; init; } = 30;

        /// <summary>
    /// 取得或設定 SecurityEventRecoveryBatchSize。
    /// </summary>
[Range(1, 1000000)]
    public int SecurityEventRecoveryBatchSize { get; init; } = 10000;
}

internal static class ServiceOptionsExtensions
{
    /// <summary>
    /// Registers strongly typed service settings and validates them during host startup.
    /// </summary>
    /// <param name="services">The service collection receiving option registrations.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>傳回 same service collection for chaining 的結果。</returns>
    internal static IServiceCollection AddIDDSCommunityOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ReportOptions>().Bind(configuration.GetSection(ReportOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<PluginOptions>().Bind(configuration.GetSection(PluginOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ProtectionOptions>().Bind(configuration.GetSection(ProtectionOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        return services;
    }
}

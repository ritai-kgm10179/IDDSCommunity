using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class DatabaseOptions
{
    internal const string SectionName = "Database";

    [Required]
    public string FileName { get; init; } = "cyberarms.idds.dbf";
}

internal sealed class ReportOptions
{
    internal const string SectionName = "Reports";

    [Range(1, 1440)]
    public int CheckIntervalMinutes { get; init; } = 10;
}

internal sealed class PluginOptions
{
    internal const string SectionName = "Plugins";

    [Required]
    public string DirectoryName { get; init; } = "Plugins";
}

internal static class ServiceOptionsExtensions
{
    /// <summary>
    /// Registers strongly typed service settings and validates them during host startup.
    /// </summary>
    /// <param name="services">The service collection receiving option registrations.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    internal static IServiceCollection AddCyberarmsOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ReportOptions>().Bind(configuration.GetSection(ReportOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<PluginOptions>().Bind(configuration.GetSection(PluginOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        return services;
    }
}

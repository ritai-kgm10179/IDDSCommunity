using Cyberarms.IntrusionDetection.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Service.Test;

[TestClass]
public sealed class RuntimeDependencyInjectionTest
{
    /// <summary>
    /// Verifies that each Host container owns a distinct runtime dependency graph instead of reusing global instances.
    /// </summary>
    [TestMethod]
    public void AddCyberarmsRuntime_CreatesContainerScopedSingletonGraph()
    {
        using ServiceProvider firstProvider = CreateProvider();
        using ServiceProvider secondProvider = CreateProvider();

        Assert.AreSame(firstProvider.GetRequiredService<Database>(), firstProvider.GetRequiredService<Database>());
        Assert.AreSame(firstProvider.GetRequiredService<IddsConfig>(), firstProvider.GetRequiredService<IddsConfig>());
        Assert.AreNotSame(firstProvider.GetRequiredService<Database>(), secondProvider.GetRequiredService<Database>());
        Assert.AreNotSame(firstProvider.GetRequiredService<IddsConfig>(), secondProvider.GetRequiredService<IddsConfig>());
        Assert.AreNotSame(firstProvider.GetRequiredService<ReportScheduler>(), secondProvider.GetRequiredService<ReportScheduler>());
        Assert.AreNotSame(firstProvider.GetRequiredService<SecurityAgents>(), secondProvider.GetRequiredService<SecurityAgents>());
    }

    /// <summary>
    /// Creates a validated runtime container with a non-COM firewall test double.
    /// </summary>
    /// <returns>The isolated service provider.</returns>
    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddCyberarmsOptions(configuration);
        services.AddSingleton<IFirewallPolicy, FakeFirewallPolicy>();
        services.AddCyberarmsRuntime();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private sealed class FakeFirewallPolicy : IFirewallPolicy
    {
        /// <summary>
        /// Records no external firewall state in dependency-registration tests.
        /// </summary>
        /// <param name="ipAddress">The address that would be blocked.</param>
        public void Block(string ipAddress)
        {
        }

        /// <summary>
        /// Reports that no address is blocked in dependency-registration tests.
        /// </summary>
        /// <param name="ipAddress">The address being queried.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        public bool IsLocked(string ipAddress) => false;

        /// <summary>
        /// Records no external firewall state in dependency-registration tests.
        /// </summary>
        /// <param name="ipAddress">The address that would be removed.</param>
        public void RemoveIpAddressFromBlockList(string ipAddress)
        {
        }
    }
}

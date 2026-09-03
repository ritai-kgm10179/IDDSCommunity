using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class RuntimeDependencyInjectionTest
{
    /// <summary>
    /// Verifies that each Host container owns a distinct runtime dependency graph instead of reusing global instances.
    /// </summary>
    [TestMethod]
    public void AddIDDSCommunityRuntime_CreatesContainerScopedSingletonGraph()
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
    /// Verifies that the registered runtime health check reports an unconfigured database as unhealthy.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task AddIDDSCommunityRuntime_RegistersRuntimeHealthCheck()
    {
        using ServiceProvider provider = CreateProvider();

        HealthReport report = await provider.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync()
            .ConfigureAwait(false);

        Assert.AreEqual(HealthStatus.Unhealthy, report.Status);
        Assert.IsTrue(report.Entries.ContainsKey("iddscommunity-runtime"));
    }
    /// <summary>
    /// Creates a validated runtime container with a non-COM firewall test double.
    /// </summary>
    /// <returns>傳回 isolated service provider 的結果。</returns>
    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddLogging();
        services.AddIDDSCommunityOptions(configuration);
        services.AddSingleton<IFirewallPolicy, FakeFirewallPolicy>();
        services.AddIDDSCommunityRuntime();
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
        /// <returns>恆傳回 <see langword="false"/>。</returns>
        public bool IsLocked(string ipAddress) => false;
        /// <summary>
        /// Returns an empty firewall snapshot for dependency-registration tests.
        /// </summary>
        /// <returns>空白的位址集合。</returns>
        public System.Collections.Generic.IReadOnlyCollection<string> GetBlockedAddresses() => [];
        /// <summary>
        /// Records no external firewall state in dependency-registration tests.
        /// </summary>
        /// <param name="ipAddress">The address that would be removed.</param>
        public void RemoveIpAddressFromBlockList(string ipAddress)
        {
        }
        /// <summary>
        /// 於相依性插入測試中模擬對齊傳入放行規則。
        /// </summary>
        /// <param name="targetRules">目標規則集合。</param>
        /// <param name="auditRecorder">稽核紀錄委派。</param>
        public void ReconcileInboundAllowRules(System.Collections.Generic.IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules, System.Action<string, string, string, string?>? auditRecorder = null)
        {
        }
        /// <summary>
        /// 於相依性插入測試中模擬清除傳入放行規則。
        /// </summary>
        /// <param name="auditRecorder">稽核紀錄委派。</param>
        public void RemoveAllInboundAllowRules(System.Action<string, string, string, string?>? auditRecorder = null)
        {
        }
    }
}

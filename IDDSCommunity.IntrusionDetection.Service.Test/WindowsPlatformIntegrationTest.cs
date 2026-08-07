using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

#pragma warning disable CA1416 // PrivilegedWindowsTestGuard limits these opt-in integration tests to supported Windows hosts.
[TestClass]
[TestCategory(PrivilegedWindowsTestGuard.Category)]
[DoNotParallelize]
public sealed class WindowsPlatformIntegrationTest
{
    /// <summary>
    /// Verifies that an elevated process can start and stop raw IPv4 packet capture without faulting its receive loop.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task RawSocketReceiver_StartAndStop_CompletesWithoutFailure()
    {
        PrivilegedWindowsTestGuard.RequireOptInAndAdministrator();
        IPAddress? address = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(item => item.AddressFamily == AddressFamily.InterNetwork);
        Assert.IsNotNull(address, "The integration host requires an IPv4 address.");

        Exception? captureException = null;
        using RawSocketReceiver receiver = new();
        receiver.CaptureFailed += (_, eventArgs) => captureException = eventArgs.Exception;
        receiver.Start(address);
        await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        receiver.Stop();
        await receiver.Completion.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.IsNull(captureException, captureException?.ToString());
    }

    /// <summary>
    /// Verifies that an isolated Application event source can be registered, written, and removed.
    /// </summary>
    [TestMethod]
    public void EventLog_CreateWriteAndDeleteSource_Succeeds()
    {
        PrivilegedWindowsTestGuard.RequireOptInAndAdministrator();
        string source = "IDDSCommunity.IntegrationTests." + Guid.NewGuid().ToString("N");
        try
        {
            EventLog.CreateEventSource(source, "Application");
            EventLog.WriteEntry(source, "IDDSCommunity privileged integration test", EventLogEntryType.Information, 1);
            Assert.IsTrue(EventLog.SourceExists(source));
        }
        finally
        {
            if (EventLog.SourceExists(source))
                EventLog.DeleteEventSource(source);
        }
    }

    /// <summary>
    /// Verifies that an isolated disabled firewall rule can be created, located, and removed without touching product rules.
    /// </summary>
    [TestMethod]
    public void Firewall_CreateAndRemoveIsolatedRule_Succeeds()
    {
        PrivilegedWindowsTestGuard.RequireOptInAndAdministrator();
        INetFwPolicy2 policy = CreateComObject<INetFwPolicy2>("HNetCfg.FwPolicy2");
        INetFwRule rule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
        string ruleName = "IDDSCommunity Integration Test " + Guid.NewGuid().ToString("N");
        try
        {
            FirewallComString.Set(ruleName, value => rule.Name = value);
            FirewallComString.Set("Temporary IDDSCommunity integration-test rule", value => rule.Description = value);
            FirewallComString.Set("IDDSCommunity Integration Tests", value => rule.Grouping = value);
            rule.Direction = NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN;
            rule.Action = NET_FW_ACTION.NET_FW_ACTION_BLOCK;
            rule.Protocol = 256;
            FirewallComString.Set("192.0.2.1", value => rule.RemoteAddresses = value);
            rule.Enabled = false;
            policy.Rules.Add(rule);

            System.Collections.Generic.List<INetFwRule> currentRules = [];
            foreach (INetFwRule item in (dynamic)policy.Rules)
                currentRules.Add(item);
            Assert.Contains(item => string.Equals(FirewallComString.Get(item.Name), ruleName, StringComparison.Ordinal), currentRules);
        }
        finally
        {
            FirewallComString.Set(ruleName, policy.Rules.Remove);
        }
    }

    /// <summary>
    /// Verifies stop and start control against a dedicated, explicitly named integration-test service.
    /// </summary>
    [TestMethod]
    public void WindowsService_StopStartAndRestore_Succeeds()
    {
        PrivilegedWindowsTestGuard.RequireOptInAndAdministrator();
        string? serviceName = Environment.GetEnvironmentVariable("IDDSCOMMUNITY_TEST_SERVICE_NAME");
        if (string.IsNullOrWhiteSpace(serviceName))
            Assert.Inconclusive("Set IDDSCOMMUNITY_TEST_SERVICE_NAME to a dedicated integration-test service name.");
        if (!serviceName.StartsWith("IDDSCommunity Integration Test", StringComparison.Ordinal))
            Assert.Fail("The integration-test service name must start with 'IDDSCommunity Integration Test'.");

        using ServiceController controller = new(serviceName);
        controller.Refresh();
        bool restoreRunning = controller.Status == ServiceControllerStatus.Running;
        try
        {
            StopIfRunning(controller);
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            Assert.AreEqual(ServiceControllerStatus.Running, controller.Status);
        }
        finally
        {
            StopIfRunning(controller);
            if (restoreRunning)
            {
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }
        }
    }

    /// <summary>
    /// Creates a Windows COM object and validates the requested interface.
    /// </summary>
    /// <typeparam name="T">The expected COM interface.</typeparam>
    /// <param name="progId">The registered COM program identifier.</param>
    /// <returns>傳回 created COM interface 的結果。</returns>
    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException($"COM type '{progId}' is unavailable.")) as T
        ?? throw new InvalidOperationException($"Unable to create COM object '{progId}'.");

    /// <summary>
    /// Stops a service when it is running or paused.
    /// </summary>
    /// <param name="controller">The dedicated integration-test service controller.</param>
    private static void StopIfRunning(ServiceController controller)
    {
        controller.Refresh();
        if (controller.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
        {
            if (controller.Status == ServiceControllerStatus.StopPending)
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            return;
        }

        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
    }
}
#pragma warning restore CA1416

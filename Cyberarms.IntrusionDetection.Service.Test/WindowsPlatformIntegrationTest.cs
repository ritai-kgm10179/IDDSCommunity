using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetFwTypeLib;

namespace Cyberarms.IntrusionDetection.Service.Test;

[TestClass]
[TestCategory(PrivilegedWindowsTestGuard.Category)]
[DoNotParallelize]
public sealed class WindowsPlatformIntegrationTest
{
    /// <summary>
    /// Verifies that an elevated process can start and stop raw IPv4 packet capture without faulting its receive loop.
    /// </summary>
    /// <returns>A task that completes after the receiver shuts down.</returns>
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
        string source = "Cyberarms.IntegrationTests." + Guid.NewGuid().ToString("N");
        try
        {
            EventLog.CreateEventSource(source, "Application");
            EventLog.WriteEntry(source, "Cyberarms privileged integration test", EventLogEntryType.Information, 1);
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
        string ruleName = "Cyberarms Integration Test " + Guid.NewGuid().ToString("N");
        try
        {
            rule.Name = ruleName;
            rule.Description = "Temporary Cyberarms integration-test rule";
            rule.Grouping = "Cyberarms Integration Tests";
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
            rule.RemoteAddresses = "192.0.2.1";
            rule.Enabled = false;
            policy.Rules.Add(rule);

            Assert.IsTrue(policy.Rules.Cast<INetFwRule>().Any(item => string.Equals(item.Name, ruleName, StringComparison.Ordinal)));
        }
        finally
        {
            policy.Rules.Remove(ruleName);
        }
    }

    /// <summary>
    /// Verifies stop and start control against a dedicated, explicitly named integration-test service.
    /// </summary>
    [TestMethod]
    public void WindowsService_StopStartAndRestore_Succeeds()
    {
        PrivilegedWindowsTestGuard.RequireOptInAndAdministrator();
        string? serviceName = Environment.GetEnvironmentVariable("CYBERARMS_TEST_SERVICE_NAME");
        if (string.IsNullOrWhiteSpace(serviceName))
            Assert.Inconclusive("Set CYBERARMS_TEST_SERVICE_NAME to a dedicated integration-test service name.");
        if (!serviceName.StartsWith("Cyberarms Integration Test", StringComparison.Ordinal))
            Assert.Fail("The integration-test service name must start with 'Cyberarms Integration Test'.");

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
    /// <returns>The created COM interface.</returns>
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

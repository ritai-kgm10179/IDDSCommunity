using System;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class AgentProxyLifecycleTest
{
    /// <summary>
    /// Verifies that disposing a proxy releases its plug-in reference and rejects later access.
    /// </summary>
    [TestMethod]
    public void Dispose_ReleasesPluginAndRejectsFurtherAccess()
    {
        string assemblyPath = typeof(DemoAgent.DemoAgent).Assembly.Location;
        string pluginRoot = Path.GetDirectoryName(assemblyPath)!;
        AgentProxy proxy = new(pluginRoot, assemblyPath, typeof(DemoAgent.DemoAgent).FullName!);

        proxy.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = proxy.IsRunning);
        proxy.Dispose();
    }
}

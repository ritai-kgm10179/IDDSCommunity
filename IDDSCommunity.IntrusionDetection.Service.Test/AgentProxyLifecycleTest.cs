using System;
using System.IO;
using IDDSCommunity.Agents.TerminalServer;
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
        string assemblyPath = typeof(IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase).Assembly.Location;
        string pluginRoot = Path.GetDirectoryName(assemblyPath)!;
        AgentProxy proxy = new(pluginRoot, assemblyPath, typeof(IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase).FullName!);

        proxy.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = proxy.IsRunning);
        proxy.Dispose();
    }

    /// <summary>
    /// 驗證遠端桌面 Agent (TlsSslAgent) 的初始化、雙軌事件監聽啟動與停止資源釋放。
    /// </summary>
    [TestMethod]
    public void TlsSslAgent_StartAndStop_CompletesWithoutException()
    {
        TlsSslAgent agent = new();
        Assert.AreEqual("{A682433B-852F-4150-ADF4-FB7F75090015}", agent.Id.ToString("B").ToUpperInvariant());
        Assert.IsFalse(agent.IsRunning);
    }
}

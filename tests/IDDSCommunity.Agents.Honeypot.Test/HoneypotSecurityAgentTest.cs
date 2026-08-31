using System;
using System.Collections;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Honeypot;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.Honeypot.Test;

/// <summary>
/// 驗證 HoneypotSecurityAgent 誘餌蜜罐代理程式之通訊埠綁定、探測偵測與事件觸發。
/// </summary>
[TestClass]
[SupportedOSPlatform("windows7.0")]
public sealed class HoneypotSecurityAgentTest
{
    /// <summary>
    /// 驗證 HoneypotConfiguration 預設通訊埠解析正確性。
    /// </summary>
    [TestMethod]
    public void HoneypotConfiguration_DefaultPorts_ParsesCorrectly()
    {
        var config = new HoneypotConfiguration();
        var ports = config.GetDecoyPorts();

        Assert.AreEqual(3, ports.Count);
        CollectionAssert.Contains((ICollection)ports, 23);
        CollectionAssert.Contains((ICollection)ports, 2222);
        CollectionAssert.Contains((ICollection)ports, 33890);
    }

    /// <summary>
    /// 驗證 HoneypotConfiguration 自訂通訊埠解析正確性。
    /// </summary>
    [TestMethod]
    public void HoneypotConfiguration_CustomPorts_ParsesCorrectly()
    {
        var config = new HoneypotConfiguration { DecoyPortsString = "8080, 9999, invalid, 65536, 0, 21" };
        var ports = config.GetDecoyPorts();

        Assert.AreEqual(3, ports.Count);
        CollectionAssert.Contains((ICollection)ports, 8080);
        CollectionAssert.Contains((ICollection)ports, 9999);
        CollectionAssert.Contains((ICollection)ports, 21);
    }

    /// <summary>
    /// 驗證 HoneypotSecurityAgent 識別碼與基本屬性。
    /// </summary>
    [TestMethod]
    public void HoneypotSecurityAgent_Metadata_MatchesWellKnownId()
    {
        var agent = new HoneypotSecurityAgent();
        Assert.AreEqual(WellKnownAgentIds.Honeypot, agent.Id);
        Assert.IsNotNull(agent.DisplayName);
    }
}

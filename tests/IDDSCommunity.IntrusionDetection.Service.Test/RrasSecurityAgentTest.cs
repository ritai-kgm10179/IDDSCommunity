using System;
using System.Reflection;
using System.Text.RegularExpressions;
using IDDSCommunity.IntrusionDetection.Base.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 RRAS 安全代理程式 (RrasSecurityAgent) 之屬性與正則表達式匹配行為。
/// </summary>
[TestClass]
public sealed class RrasSecurityAgentTest
{
    /// <summary>
    /// 驗證 RRAS Agent 能正確被具現化且具備預期的 GUID 識別碼。
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesExpectedProperties()
    {
        RrasSecurityAgent agent = new();
        Assert.AreEqual(new Guid("{FDA41145-2E75-400E-882C-E06EC4790EBE}"), agent.Id);
        Assert.IsNotNull(agent.DisplayName);
    }

    /// <summary>
    /// 驗證 MyRegex 規則運算式能精準匹配 IPv4 位址，且因點號已正確跳脫，不會將非小數點之分隔字元誤判為 IP。
    /// </summary>
    [TestMethod]
    public void MyRegex_MatchesExactIpv4AndRejectsInvalidSeparators()
    {
        MethodInfo? method = typeof(RrasSecurityAgent).GetMethod("MyRegex", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "MyRegex method must exist.");

        Regex regex = (Regex)method.Invoke(null, null)!;
        Assert.IsNotNull(regex);

        // 合法 IPv4 應正確比對成功
        Match validMatch = regex.Match("Client connected from 192.168.1.100 port 443");
        Assert.IsTrue(validMatch.Success);
        Assert.AreEqual("192.168.1.100", validMatch.Value);

        // 分隔符號非小數點（如 192a168b1c100）必須比對失敗，證明點號已正確跳脫
        Match invalidMatch = regex.Match("Client connected from 192a168b1c100 port 443");
        Assert.IsFalse(invalidMatch.Success);
    }
}
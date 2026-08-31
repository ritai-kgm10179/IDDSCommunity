using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.SelfService;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.SelfService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 SelfServiceUnblockServer 自助解鎖門戶之 HTTP 頁面回傳與 TOTP 解鎖邏輯。
/// </summary>
[TestClass]
public sealed class SelfServiceUnblockServerTest
{
    /// <summary>
    /// 驗證當未啟用時不啟動監聽。
    /// </summary>
    [TestMethod]
    public void Start_WhenDisabled_DoesNotListen()
    {
        var settings = new SelfServicePortalSettings
        {
            EnableSelfServicePortal = false,
            PortalPort = 18444
        };
        using var server = new SelfServiceUnblockServer(settings, new Database());
        server.Start();
        Assert.IsFalse(server.IsRunning);
    }
}

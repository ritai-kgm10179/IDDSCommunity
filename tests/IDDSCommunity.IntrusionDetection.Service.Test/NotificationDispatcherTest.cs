using System;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.Notifications;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 NotificationDispatcher 統一分發中樞功能。
/// </summary>
[TestClass]
public sealed class NotificationDispatcherTest
{
    /// <summary>
    /// 驗證 DispatchAlertAsync 能夠順暢非同步分發而不拋出未攔截例外。
    /// </summary>
    [TestMethod]
    public async Task DispatchAlertAsync_DispatchesGracefully()
    {
        var config = new IddsConfig(new Database());
        var notificationSettings = new NotificationSettings(config);

        var emailService = new EmailNotificationService(config, notificationSettings);
        var webhookService = new WebhookNotificationService(notificationSettings);
        var syslogService = new SyslogNotificationService(notificationSettings);
        var soarExecutor = new SoarRemediationExecutor(config);

        using var dispatcher = new NotificationDispatcher(emailService, webhookService, syslogService, soarExecutor);

        // 驗證子服務皆已就緒
        Assert.IsNotNull(dispatcher.EmailService);
        Assert.IsNotNull(dispatcher.WebhookService);
        Assert.IsNotNull(dispatcher.SyslogService);
        Assert.IsNotNull(dispatcher.SoarExecutor);

        // 在未配置伺服器環境下應優雅失敗並回傳，不造成崩潰
        await dispatcher.DispatchAlertAsync(LockType.HardLock, "198.51.100.12", "TestAgent", "Test attack alert");
    }
}

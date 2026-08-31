using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.Notifications;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 WebhookNotificationService 之警報分發與 HTTP 呼叫邏輯。
/// </summary>
[TestClass]
public sealed class WebhookNotificationServiceTest
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastContent { get; private set; }
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastContent = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return new HttpResponseMessage(StatusCodeToReturn);
        }
    }

    /// <summary>
    /// 驗證當 Webhook 停用時不發送任何請求。
    /// </summary>
    [TestMethod]
    public async Task SendWebhookAlertAsync_Disabled_ReturnsFalseWithoutRequest()
    {
        var settings = new NotificationSettings(new IddsConfig(new Database()))
        {
            EnableWebhook = false,
            WebhookPlatform = WebhookPlatform.Slack,
            WebhookUrl = "https://hooks.slack.com/services/test"
        };

        var handler = new MockHttpMessageHandler();
        using var client = new HttpClient(handler);
        using var service = new WebhookNotificationService(settings, client);

        bool result = await service.SendWebhookAlertAsync(LockType.HardLock, "198.51.100.5", "RDP", "Failed login.");

        Assert.IsFalse(result);
        Assert.IsNull(handler.LastRequest);
    }

    /// <summary>
    /// 驗證當 Webhook 啟用且為 HardLock 時正確發送至目標 URL。
    /// </summary>
    [TestMethod]
    public async Task SendWebhookAlertAsync_EnabledHardLock_SendsRequest()
    {
        var settings = new NotificationSettings(new IddsConfig(new Database()))
        {
            EnableWebhook = true,
            WebhookPlatform = WebhookPlatform.Discord,
            WebhookUrl = "https://discord.com/api/webhooks/test",
            WebhookOnHardLock = true
        };

        var handler = new MockHttpMessageHandler();
        using var client = new HttpClient(handler);
        using var service = new WebhookNotificationService(settings, client);

        bool result = await service.SendWebhookAlertAsync(LockType.HardLock, "203.0.113.10", "OpenSSH", "SSH attack.");

        Assert.IsTrue(result);
        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual("https://discord.com/api/webhooks/test", handler.LastRequest.RequestUri?.ToString());
        Assert.IsTrue(handler.LastContent?.Contains("203.0.113.10") ?? false);
    }

    /// <summary>
    /// 驗證 Telegram 平台時正確組合 Bot API URL。
    /// </summary>
    [TestMethod]
    public async Task SendWebhookAlertAsync_Telegram_ConstructsBotApiUrl()
    {
        var settings = new NotificationSettings(new IddsConfig(new Database()))
        {
            EnableWebhook = true,
            WebhookPlatform = WebhookPlatform.Telegram,
            TelegramBotToken = "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
            TelegramChatId = "-10099887766",
            WebhookOnHardLock = true
        };

        var handler = new MockHttpMessageHandler();
        using var client = new HttpClient(handler);
        using var service = new WebhookNotificationService(settings, client);

        bool result = await service.SendWebhookAlertAsync(LockType.HardLock, "198.51.100.22", "FTP", "Brute force.");

        Assert.IsTrue(result);
        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual("https://api.telegram.org/bot123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11/sendMessage", handler.LastRequest.RequestUri?.ToString());
        Assert.IsTrue(handler.LastContent?.Contains("-10099887766") ?? false);
    }
}

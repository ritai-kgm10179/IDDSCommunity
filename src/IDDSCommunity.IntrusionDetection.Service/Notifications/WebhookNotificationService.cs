using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;

namespace IDDSCommunity.IntrusionDetection.Service.Notifications;

/// <summary>
/// 提供多渠道 Webhook（Teams、Slack、Discord、Telegram）非同步警報推送服務。
/// </summary>
public sealed class WebhookNotificationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly NotificationSettings _settings;

    /// <summary>
    /// 初始化 <see cref="WebhookNotificationService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">通知設定執行個體。</param>
    /// <param name="httpClient">選擇性注入之 HttpClient 執行個體。</param>
    public WebhookNotificationService(NotificationSettings settings, HttpClient? httpClient = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _ownsHttpClient = true;
        }
    }

    /// <summary>
    /// 依據鎖定事件類型非同步發送 Webhook 警報通知。
    /// </summary>
    /// <param name="lockType">鎖定類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">觸發代理程式名稱。</param>
    /// <param name="details">事件詳細說明。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>表示非同步發送作業的 Task。</returns>
    public async Task<bool> SendWebhookAlertAsync(
        LockType lockType,
        string ipAddress,
        string agentName,
        string details,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableWebhook || _settings.WebhookPlatform == WebhookPlatform.None)
            return false;

        bool shouldSend = lockType switch
        {
            LockType.SoftLock => _settings.WebhookOnSoftLock,
            LockType.HardLock => _settings.WebhookOnHardLock,
            LockType.None => _settings.WebhookOnUnlock,
            _ => false
        };

        if (!shouldSend)
            return false;

        string eventTitle = lockType switch
        {
            LockType.SoftLock => Strings.Get("Soft lock"),
            LockType.HardLock => Strings.Get("Hard lock"),
            _ => Strings.Get("Unlocked")
        };

        string statusName = LockStatusAdapter.GetLockStatusName((int)lockType);

        return await SendPayloadAsync(eventTitle, ipAddress, statusName, agentName, details, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 發送測試 Webhook 訊息以驗證端點連通性。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若發送成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public async Task<bool> TestWebhookAsync(CancellationToken cancellationToken = default)
    {
        string eventTitle = Strings.Get("AttackDetected") + " (Test)";
        string ipAddress = "203.0.113.199";
        string statusName = Strings.Get("Hard lock");
        string agentName = Strings.AppTitle;
        string details = Strings.Get("Configuration was saved successfully.") + " Webhook test notification.";

        return await SendPayloadAsync(eventTitle, ipAddress, statusName, agentName, details, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> SendPayloadAsync(
        string eventTitle,
        string ipAddress,
        string statusName,
        string agentName,
        string details,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        try
        {
            string url = GetTargetUrl();
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string jsonPayload = WebhookPayloadBuilder.BuildPayload(
                _settings.WebhookPlatform,
                eventTitle,
                ipAddress,
                statusName,
                agentName,
                details,
                timestamp,
                _settings.TelegramChatId);

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string GetTargetUrl()
    {
        if (_settings.WebhookPlatform == WebhookPlatform.Telegram)
        {
            if (string.IsNullOrWhiteSpace(_settings.TelegramBotToken))
                return string.Empty;

            return $"https://api.telegram.org/bot{_settings.TelegramBotToken.Trim()}/sendMessage";
        }

        return _settings.WebhookUrl.Trim();
    }

    /// <summary>
    /// 釋放未受控資源。
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}

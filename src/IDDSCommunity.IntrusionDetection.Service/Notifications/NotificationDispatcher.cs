using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;

namespace IDDSCommunity.IntrusionDetection.Service.Notifications;

/// <summary>
/// 提供統一之資安警報、SOAR 處置與排程報表多渠道非同步分發中樞。
/// </summary>
public sealed class NotificationDispatcher : IDisposable
{
    private readonly EmailNotificationService emailService;
    private readonly WebhookNotificationService webhookService;
    private readonly SyslogNotificationService syslogService;
    private readonly SoarRemediationExecutor soarExecutor;

    /// <summary>
    /// 初始化 <see cref="NotificationDispatcher"/> 類別的新執行個體。
    /// </summary>
    /// <param name="emailService">郵件通知服務。</param>
    /// <param name="webhookService">Webhook 即時通訊通知服務。</param>
    /// <param name="syslogService">Syslog / CEF SIEM 轉發服務。</param>
    /// <param name="soarExecutor">SOAR 自訂處置執行器。</param>
    public NotificationDispatcher(
        EmailNotificationService emailService,
        WebhookNotificationService webhookService,
        SyslogNotificationService syslogService,
        SoarRemediationExecutor soarExecutor)
    {
        this.emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        this.webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        this.syslogService = syslogService ?? throw new ArgumentNullException(nameof(syslogService));
        this.soarExecutor = soarExecutor ?? throw new ArgumentNullException(nameof(soarExecutor));
    }

    /// <summary>
    /// 取得底層電子郵件通知服務執行個體。
    /// </summary>
    public EmailNotificationService EmailService => emailService;

    /// <summary>
    /// 取得底層 Webhook 通知服務執行個體。
    /// </summary>
    public WebhookNotificationService WebhookService => webhookService;

    /// <summary>
    /// 取得底層 Syslog 通知服務執行個體。
    /// </summary>
    public SyslogNotificationService SyslogService => syslogService;

    /// <summary>
    /// 取得底層 SOAR 處置執行器執行個體。
    /// </summary>
    public SoarRemediationExecutor SoarExecutor => soarExecutor;

    /// <summary>
    /// 統一將資安事件非同步分發至所有已啟用的出站通知通道（E-Mail, Webhook, Syslog/CEF, SOAR 處置腳本）。
    /// </summary>
    /// <param name="lockType">鎖定操作類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">觸發之安全性代理程式名稱。</param>
    /// <param name="details">詳細事件描述。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    public async Task DispatchAlertAsync(
        LockType lockType,
        string ipAddress,
        string agentName,
        string details,
        CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];

        // 1. E-Mail 警報通知
        tasks.Add(emailService.SendAlertEmailAsync(lockType, ipAddress, agentName, details, cancellationToken));

        // 2. Webhook (Teams / Slack / Discord / Telegram / LINE / Generic)
        tasks.Add(webhookService.SendWebhookAlertAsync(lockType, ipAddress, agentName, details, cancellationToken));

        // 3. Syslog / Micro Focus CEF
        tasks.Add(syslogService.SendSyslogAlertAsync(lockType, ipAddress, agentName, details, cancellationToken));

        // 4. SOAR 自訂處置腳本 (PowerShell / CMD)
        if (lockType == LockType.HardLock)
        {
            tasks.Add(soarExecutor.ExecuteScriptAsync(lockType, ipAddress, agentName, details, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[NotificationDispatcher] Alert dispatch encountered error: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
    }

    /// <summary>
    /// 釋放服務使用之受控與未受控資源。
    /// </summary>
    public void Dispose()
    {
        webhookService.Dispose();
        syslogService.Dispose();
    }
}

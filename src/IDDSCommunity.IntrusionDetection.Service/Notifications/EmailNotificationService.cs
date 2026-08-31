using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IDDSCommunity.IntrusionDetection.Service.Notifications;

/// <summary>
/// 提供 SMTP / MailKit 郵件傳輸、身分驗證、HTML 警報模板與排程報表寄送之獨立通知服務。
/// </summary>
public sealed class EmailNotificationService
{
    private readonly IddsConfig configuration;
    private readonly NotificationSettings notificationSettings;

    /// <summary>
    /// 初始化 <see cref="EmailNotificationService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="configuration">應用程式全域組態。</param>
    /// <param name="notificationSettings">通知設定執行個體。</param>
    public EmailNotificationService(IddsConfig configuration, NotificationSettings notificationSettings)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.notificationSettings = notificationSettings ?? throw new ArgumentNullException(nameof(notificationSettings));
    }

    /// <summary>
    /// 依據鎖定操作類型非同步發送電子郵件資安警報。
    /// </summary>
    /// <param name="lockType">鎖定操作類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">觸發之安全性代理程式名稱。</param>
    /// <param name="message">詳細事件說明。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若成功發送傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public async Task<bool> SendAlertEmailAsync(
        LockType lockType,
        string ipAddress,
        string agentName,
        string message,
        CancellationToken cancellationToken = default)
    {
        string subject = string.Empty;
        switch (lockType)
        {
            case LockType.None:
                if (notificationSettings.OnUnlock)
                    subject = Strings.Format("IDDS Community: Unlock notification ({0})", ipAddress);
                break;
            case LockType.SoftLock:
                if (notificationSettings.OnSoftLock)
                    subject = Strings.Format("IDDS Community: Soft lock notification ({0})", ipAddress);
                break;
            case LockType.HardLock:
                if (notificationSettings.OnHardLock)
                    subject = Strings.Format("IDDS Community: Hard lock notification ({0})", ipAddress);
                break;
        }

        if (string.IsNullOrEmpty(subject))
            return false;

        string htmlBody = $$"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Segoe UI,Roboto,sans-serif;background-color:#f8fafc;color:#1e293b;padding:20px;">
          <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;border:1px solid #e2e8f0;padding:24px;box-shadow:0 4px 6px rgba(0,0,0,0.05);">
            <div style="border-bottom:2px solid #0ea5e9;padding-bottom:12px;margin-bottom:16px;">
              <h2 style="margin:0;color:#0f172a;font-size:18px;">🛡️ IDDS Community - {{subject}}</h2>
            </div>
            <p><strong>來源 IP 位址：</strong> <code style="background:#f1f5f9;padding:2px 6px;border-radius:4px;color:#0284c7;">{{ipAddress}}</code></p>
            <p><strong>安全代理程式：</strong> {{agentName}}</p>
            <p><strong>事件發生時間 (UTC)：</strong> {{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}}</p>
            <div style="background:#f8fafc;border-left:4px solid #0ea5e9;padding:12px;margin:16px 0;font-size:14px;color:#334155;">
              {{message}}
            </div>
            <hr style="border:none;border-top:1px solid #e2e8f0;margin:20px 0;"/>
            <p style="font-size:12px;color:#94a3b8;margin:0;text-align:center;">
              本郵件由 IDDS Community 入侵防禦系統自動發出，請勿直接回覆。
            </p>
          </div>
        </body>
        </html>
        """;

        return await SendMailAsync(subject, htmlBody, isHtml: true, cancellationToken: cancellationToken, rethrowOnFailure: false).ConfigureAwait(false);
    }

    /// <summary>
    /// 發送一般郵件或排程資安報表郵件。
    /// </summary>
    /// <param name="subject">郵件主旨。</param>
    /// <param name="message">郵件內文。</param>
    /// <param name="isHtml">內文是否為 HTML 格式。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <param name="rethrowOnFailure">若發送失敗是否重新拋出例外狀況。</param>
    /// <returns>若成功發送傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public async Task<bool> SendMailAsync(
        string subject,
        string message,
        bool isHtml,
        CancellationToken cancellationToken = default,
        bool rethrowOnFailure = false)
    {
        try
        {
            if (string.IsNullOrEmpty(configuration.SmtpServer) || string.IsNullOrEmpty(configuration.SenderEmailAddress)
                || string.IsNullOrEmpty(configuration.NotificationEmailAddress))
            {
                if (rethrowOnFailure)
                    throw new InvalidOperationException(Strings.Get("SMTP configuration is incomplete."));
                return false;
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(configuration.SenderEmailAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(configuration.NotificationEmailAddress));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart(isHtml ? "html" : "plain") { Text = message };

            using var client = new SmtpClient();
            int port = configuration.SmtpPort == 0 ? 25 : configuration.SmtpPort;
            SecureSocketOptions secureOption = configuration.SmtpSslRequired ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));

            await client.ConnectAsync(configuration.SmtpServer, port, secureOption, timeout.Token).ConfigureAwait(false);

            if (configuration.SmtpRequiresAuthentication)
            {
                await client.AuthenticateAsync(configuration.SmtpUsername, configuration.GetSmtpPassword(), timeout.Token).ConfigureAwait(false);
            }

            await client.SendAsync(mimeMessage, timeout.Token).ConfigureAwait(false);
            await client.DisconnectAsync(true, timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            if (rethrowOnFailure)
                throw;
            return false;
        }
    }
}

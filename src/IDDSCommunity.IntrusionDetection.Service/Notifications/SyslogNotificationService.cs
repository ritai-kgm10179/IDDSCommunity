using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;

namespace IDDSCommunity.IntrusionDetection.Service.Notifications;

/// <summary>
/// 提供將入侵防護事件透過 UDP、TCP 或 TLS 協定轉送至遠端 Syslog / SIEM 伺服器之背景服務。
/// </summary>
public sealed class SyslogNotificationService : IDisposable
{
    private readonly NotificationSettings settings;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private bool disposed;

    /// <summary>
    /// 初始化 <see cref="SyslogNotificationService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">通知設定模型。</param>
    public SyslogNotificationService(NotificationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
    }

    /// <summary>
    /// 依據設定與事件類型非同步發送 Syslog 訊息。
    /// </summary>
    /// <param name="lockType">封鎖類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">代理程式名稱。</param>
    /// <param name="details">詳細說明。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若發送成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public async Task<bool> SendSyslogAlertAsync(
        LockType lockType,
        string ipAddress,
        string agentName,
        string details,
        CancellationToken cancellationToken = default)
    {
        if (!settings.EnableSyslog || string.IsNullOrWhiteSpace(settings.SyslogHost))
            return false;

        bool shouldSend = lockType switch
        {
            LockType.SoftLock => settings.SyslogOnSoftLock,
            LockType.HardLock => settings.SyslogOnHardLock,
            LockType.None => settings.SyslogOnUnlock,
            _ => false
        };

        if (!shouldSend)
            return false;

        string message = SyslogPayloadBuilder.BuildMessage(
            settings.SyslogFormat,
            lockType,
            ipAddress,
            agentName,
            details,
            DateTime.UtcNow);

        return await SendRawMessageAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 發送測試 Syslog 訊息以驗證端點連通性。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若發送成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public async Task<bool> TestSyslogAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.SyslogHost))
            return false;

        string message = SyslogPayloadBuilder.BuildMessage(
            settings.SyslogFormat,
            LockType.HardLock,
            "203.0.113.199",
            "IDDS Community",
            "IDDS Community Syslog connectivity test message.",
            DateTime.UtcNow);

        return await SendRawMessageAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> SendRawMessageAsync(string message, CancellationToken cancellationToken)
    {
        await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            string host = settings.SyslogHost.Trim();
            int port = settings.SyslogPort > 0 ? settings.SyslogPort : 514;

            if (settings.SyslogProtocol == SyslogProtocol.Udp)
            {
                using var udpClient = new UdpClient();
                await udpClient.SendAsync(data, data.Length, host, port).ConfigureAwait(false);
                return true;
            }
            else if (settings.SyslogProtocol == SyslogProtocol.Tcp)
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
                using var stream = tcpClient.GetStream();
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            else if (settings.SyslogProtocol == SyslogProtocol.Tls)
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
                using var sslStream = new SslStream(tcpClient.GetStream(), false, (_, _, _, _) => true);
                await sslStream.AuthenticateAsClientAsync(host).ConfigureAwait(false);
                await sslStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await sslStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Failed to send Syslog to {0}:{1} - {2}", settings.SyslogHost, settings.SyslogPort, ex.Message);
            return false;
        }
        finally
        {
            sendLock.Release();
        }
    }

    /// <summary>
    /// 釋放非受控資源。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        sendLock.Dispose();
        disposed = true;
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.Notifications;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 SyslogNotificationService 透過 UDP 發送 Syslog 訊息與連通性測試。
/// </summary>
[TestClass]
public sealed class SyslogNotificationServiceTest
{
    /// <summary>
    /// 驗證當 EnableSyslog 為 false 時不發送 Syslog。
    /// </summary>
    [TestMethod]
    public async Task SendSyslogAlertAsync_WhenDisabled_ReturnsFalse()
    {
        var config = new IddsConfig(new Database());
        var settings = new NotificationSettings(config)
        {
            EnableSyslog = false,
            SyslogHost = "127.0.0.1",
            SyslogPort = 514
        };

        using var service = new SyslogNotificationService(settings);
        bool sent = await service.SendSyslogAlertAsync(LockType.HardLock, "198.51.100.1", "TestAgent", "Test");
        Assert.IsFalse(sent);
    }

    /// <summary>
    /// 驗證當 EnableSyslog 為 true 且為 UDP 協定時成功發送至本機 UDP 監聽器。
    /// </summary>
    [TestMethod]
    public async Task SendSyslogAlertAsync_Udp_SendsSuccessfully()
    {
        using var udpListener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)udpListener.Client.LocalEndPoint!).Port;

        var config = new IddsConfig(new Database());
        var settings = new NotificationSettings(config)
        {
            EnableSyslog = true,
            SyslogHost = "127.0.0.1",
            SyslogPort = port,
            SyslogProtocol = SyslogProtocol.Udp,
            SyslogFormat = SyslogFormat.Rfc5424,
            SyslogOnHardLock = true
        };

        using var service = new SyslogNotificationService(settings);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var receiveTask = udpListener.ReceiveAsync(cts.Token).AsTask();
        bool sent = await service.SendSyslogAlertAsync(LockType.HardLock, "198.51.100.1", "TestAgent", "Test message", cts.Token);

        Assert.IsTrue(sent);
        var receiveResult = await receiveTask;
        string received = Encoding.UTF8.GetString(receiveResult.Buffer);
        Assert.IsTrue(received.Contains("198.51.100.1", StringComparison.Ordinal));
        Assert.IsTrue(received.Contains("TestAgent", StringComparison.Ordinal));
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.Agents.Honeypot;

/// <summary>
/// 提供主動式誘餌蜜罐安全性代理程式，監聽未使用的通訊埠並在接收到主動探測連線時立即觸發防護。
/// </summary>
[SupportedOSPlatform("windows7.0")]
[Plugin("Honeypot Decoy Security Agent", "Detects active port-scan and unauthorized connection probes on decoy ports.", "1.0")]
public sealed class HoneypotSecurityAgent : AgentPlugin, IExtendedInformation
{
    private readonly List<TcpListener> listeners = [];
    private CancellationTokenSource? cts;

    /// <summary>
    /// 初始化 <see cref="HoneypotSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public HoneypotSecurityAgent()
    {
        HoneypotConfiguration settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName { get => Strings.Get("Honeypot Decoy Security Agent"); set { } }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public Guid Id => WellKnownAgentIds.Honeypot;

    /// <summary>
    /// 取得或設定 Agent 的預設圖示。
    /// </summary>
    public Image? Icon { get; set; }

    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? SelectedIcon { get; set; }

    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? UnselectedIcon { get; set; }

    /// <summary>
    /// 處理啟動 Agent 的通知。
    /// </summary>
    protected override void OnStartAgent()
    {
        StopListeners();
        cts = new CancellationTokenSource();

        HoneypotConfiguration settings = Configuration.AgentSettings as HoneypotConfiguration ?? new HoneypotConfiguration();
        var ports = settings.GetDecoyPorts();

        foreach (int port in ports)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listeners.Add(listener);
                _ = ListenAsync(listener, port, cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Failed to bind honeypot listener on port {0}: {1}", port, ex.Message);
            }
        }

        base.OnStartAgent();
    }

    /// <summary>
    /// 處理停止 Agent 的通知。
    /// </summary>
    protected override void OnStopAgent()
    {
        StopListeners();
        base.OnStopAgent();
    }

    private void StopListeners()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        foreach (var listener in listeners)
        {
            try { listener.Stop(); }
            catch { }
        }
        listeners.Clear();
    }

    private async Task ListenAsync(TcpListener listener, int port, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using TcpClient client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                if (client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
                {
                    IPAddress remoteIp = remoteEndPoint.Address;

                    // 檢查 IPv4 映射與 Bogon 過濾
                    if (remoteIp.IsIPv4MappedToIPv6)
                        remoteIp = remoteIp.MapToIPv4();

                    if (!BogonIpFilter.IsBogonOrReserved(remoteIp))
                    {
                        string message = Strings.Format("Honeypot decoy port probe detected on TCP port {0}.", port);
                        NotificationEventArgs args = new()
                        {
                            CreateDate = DateTime.Now,
                            EventId = 9920,
                            EventMessage = message,
                            IpAddress = remoteIp.ToString()
                        };

                        OnAttackDetected(this, args);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested)
                    break;
                System.Diagnostics.Trace.TraceWarning("Honeypot accept exception on port {0}: {1}", port, ex.Message);
            }
        }
    }
}

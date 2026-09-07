using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 提供符合微軟 .NET 官方最佳實踐、基於 <see cref="SocketsHttpHandler"/>、連線池生命週期管理與 Socket 連線層 IMDS 防禦之 HTTP 用戶端建立協助工具。
/// </summary>
public static class HttpClientHelper
{
    /// <summary>
    /// 預設連線池連線存活期限（15 分鐘），可有效防止 DNS 快取陳舊（DNS Staleness）。
    /// </summary>
    public static readonly TimeSpan DefaultPooledConnectionLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// 建立具備 DNS 重新解析連線池管理與 Socket 連線層 IMDS 防禦能力之長生命週期 <see cref="HttpClient"/> 執行個體。
    /// </summary>
    /// <param name="timeout">逾時時間間隔；若為 <see langword="null"/> 則預設為 30 秒。</param>
    /// <param name="pooledLifetime">連線池生命週期；若為 <see langword="null"/> 則預設為 15 分鐘。</param>
    /// <param name="userAgent">自訂 User-Agent 標頭字串。</param>
    /// <param name="blockImdsAndLinkLocal">指出是否在 Socket 連線握手前強制阻絕雲端 IMDS 與 Link-Local 位址（預設為 <see langword="true"/>）。</param>
    /// <param name="customBlockFilter">選擇性的自訂 IP 阻絕過濾委派；若傳回 <see langword="true"/> 則拒絕連線握手。</param>
    /// <returns>已完成設定之 <see cref="HttpClient"/> 執行個體。</returns>
    public static HttpClient CreatePooledClient(
        TimeSpan? timeout = null,
        TimeSpan? pooledLifetime = null,
        string? userAgent = null,
        bool blockImdsAndLinkLocal = true,
        Func<IPAddress, bool>? customBlockFilter = null)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = pooledLifetime ?? DefaultPooledConnectionLifetime
        };

        if (blockImdsAndLinkLocal || customBlockFilter != null)
        {
            handler.ConnectCallback = async (context, cancellationToken) =>
            {
                DnsEndPoint dnsEndPoint = context.DnsEndPoint;
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(dnsEndPoint.Host, dnsEndPoint.AddressFamily, cancellationToken).ConfigureAwait(false);
                foreach (IPAddress address in addresses)
                {
                    if (blockImdsAndLinkLocal && NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(address))
                    {
                        throw new InvalidOperationException(string.Format(
                            global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Connection to IMDS or link-local address '{0}' is blocked."),
                            address));
                    }

                    if (customBlockFilter != null && customBlockFilter(address))
                    {
                        throw new InvalidOperationException(string.Format(
                            global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Connection to restricted or Bogon IP address '{0}' is blocked."),
                            address));
                    }
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, dnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };
        }

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        return client;
    }
}
using System;
using System.Net.Http;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 提供符合微軟 .NET 官方最佳實踐、基於 <see cref="SocketsHttpHandler"/> 與連線池生命週期管理之 HTTP 用戶端建立協助工具。
/// </summary>
public static class HttpClientHelper
{
    /// <summary>
    /// 預設連線池連線存活期限（15 分鐘），可有效防止 DNS 快取陳舊（DNS Staleness）。
    /// </summary>
    public static readonly TimeSpan DefaultPooledConnectionLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// 建立具備 DNS 重新解析連線池管理能力之長生命週期 <see cref="HttpClient"/> 執行個體。
    /// </summary>
    /// <param name="timeout">逾時時間間隔；若為 <see langword="null"/> 則預設為 30 秒。</param>
    /// <param name="pooledLifetime">連線池生命週期；若為 <see langword="null"/> 則預設為 15 分鐘。</param>
    /// <param name="userAgent">自訂 User-Agent 標頭字串。</param>
    /// <returns>已完成設定之 <see cref="HttpClient"/> 執行個體。</returns>
    public static HttpClient CreatePooledClient(
        TimeSpan? timeout = null,
        TimeSpan? pooledLifetime = null,
        string? userAgent = null)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = pooledLifetime ?? DefaultPooledConnectionLifetime
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        return client;
    }
}
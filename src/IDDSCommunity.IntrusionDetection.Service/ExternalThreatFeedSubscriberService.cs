using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供外部惡意 IP 威脅情資（Threat Feeds）定期自動訂閱下載、Bogon 過濾、安全網路檢驗與主動防護同步服務。
/// </summary>
internal sealed class ExternalThreatFeedSubscriberService : IDisposable
{
    private const string DefaultUserAgent = "IDDSCommunity-ThreatFeed-Subscriber/3.0 (+https://github.com/ritai-kgm10179/IDDSCommunity)";
    private readonly IddsConfig config;
    private readonly Action<ThreatIntelligenceItem> onThreatDiscovered;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logWarning;
    private readonly Action<string, string, string, string?>? recordAudit;
    private readonly HttpClient httpClient;
    private readonly bool ownClient;

    private System.Threading.Timer? refreshTimer;
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private volatile bool disposed;

    /// <summary>
    /// 初始化 <see cref="ExternalThreatFeedSubscriberService"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域設定執行個體。</param>
    /// <param name="onThreatDiscovered">當解析出通過安全過濾之新威脅 IP 時引發之回呼委派。</param>
    /// <param name="logInformation">資訊日誌回報委派。</param>
    /// <param name="logWarning">警告日誌回報委派。</param>
    /// <param name="httpClient">可選之自訂 HttpClient 執行個體（用於單元測試隔離）。</param>
    /// <param name="recordAudit">可選之稽核日誌回報委派。</param>
    public ExternalThreatFeedSubscriberService(
        IddsConfig config,
        Action<ThreatIntelligenceItem> onThreatDiscovered,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null,
        HttpClient? httpClient = null,
        Action<string, string, string, string?>? recordAudit = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.onThreatDiscovered = onThreatDiscovered ?? throw new ArgumentNullException(nameof(onThreatDiscovered));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logWarning = logWarning ?? ((msg, ex) => System.Diagnostics.Trace.TraceWarning("{0}: {1}", msg, ex.Message));
        this.recordAudit = recordAudit;

        if (httpClient != null)
        {
            this.httpClient = httpClient;
            ownClient = false;
        }
        else
        {
            this.httpClient = IDDSCommunity.IntrusionDetection.Shared.Network.HttpClientHelper.CreatePooledClient(TimeSpan.FromSeconds(30), userAgent: DefaultUserAgent);
            ownClient = true;
        }
    }

    /// <summary>
    /// 啟動外部威脅情報定期訂閱與更新排程。
    /// </summary>
    public void Start()
    {
        if (disposed) return;
        int intervalHours = Math.Max(1, config.ThreatFeedUpdateIntervalHours);
        refreshTimer = new System.Threading.Timer(
            async _ => await RefreshFeedsAsync().ConfigureAwait(false),
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromHours(intervalHours));
    }

    /// <summary>
    /// 立即非同步執行一次外部威脅情資下載、解析與過濾同步。
    /// </summary>
    /// <returns>表示非同步作業完成之 Task。</returns>
    public async Task RefreshFeedsAsync()
    {
        if (disposed || stopping.IsCancellationRequested || !await refreshGate.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            if (!config.EnableExternalThreatFeeds)
                return;

            // 邊緣節點（EdgeNode）一律由 ThreatHub 集中同步，不重複對外下載 Feed
            if (config.ThreatHubRole == ThreatHubRole.EdgeNode)
                return;

            logInformation("Starting external threat intelligence feed update cycle...");
            int totalIngested = 0;
            int ttlDays = Math.Clamp(config.ThreatFeedTtlDays, 1, 365);
            DateTime expiresUtc = DateTime.UtcNow.AddDays(ttlDays);

            // 0. 更新動態 Bogon 前綴清單（Team Cymru Fullbogons）
            if (config.EnableDynamicBogonUpdate)
            {
                try
                {
                    await RefreshDynamicBogonsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logWarning("Failed to update dynamic Bogon prefix list", ex);
                }
            }

            // 1. 抓取預設開源 IPsum 分級清單
            try
            {
                int level = Math.Clamp(config.ThreatFeedMinLevel, 1, 8);
                string ipsumUrl = $"https://raw.githubusercontent.com/stamparm/ipsum/master/levels/{level}.txt";
                totalIngested += await ProcessSingleFeedAsync(
                    "IPsum (Aggregated Threat Feed)",
                    ipsumUrl,
                    ThreatFeedFormat.IPsumTabDelimited,
                    level,
                    expiresUtc).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logWarning("Failed to update IPsum threat feed", ex);
            }

            // 2. 抓取 AbuseIPDB Blacklist（若有提供 API Key）
            if (!string.IsNullOrWhiteSpace(config.AbuseIpDbApiKey))
            {
                try
                {
                    int minConfidence = Math.Clamp(config.AbuseIpDbMinConfidence, 25, 100);
                    string abuseUrl = $"https://api.abuseipdb.com/api/v2/blacklist?confidenceMinimum={minConfidence}&limit=10000";
                    totalIngested += await ProcessAbuseIpDbFeedAsync(abuseUrl, config.AbuseIpDbApiKey, minConfidence, expiresUtc).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logWarning("Failed to update AbuseIPDB threat feed", ex);
                }
            }

            // 3. 抓取使用者自訂 URLs
            string customUrlsRaw = config.ThreatFeedCustomUrls;
            if (!string.IsNullOrWhiteSpace(customUrlsRaw))
            {
                string[] urls = customUrlsRaw.Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string url in urls)
                {
                    string trimmedUrl = url.Trim();
                    if (string.IsNullOrEmpty(trimmedUrl) || !trimmedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!await IsSafeExternalUrlAsync(trimmedUrl, stopping.Token).ConfigureAwait(false))
                    {
                        logWarning($"Blocked unsafe or SSRF-suspect custom threat feed URL: '{trimmedUrl}'", new InvalidOperationException("SSRF validation failed"));
                        recordAudit?.Invoke("ThreatFeed.Download", "Blocked", $"CustomFeed ({trimmedUrl})", "Blocked by SSRF filter (private/bogon/IMDS destination)");
                        continue;
                    }

                    try
                    {
                        totalIngested += await ProcessSingleFeedAsync(
                            $"CustomFeed ({trimmedUrl})",
                            trimmedUrl,
                            ThreatFeedFormat.PlainTextLines,
                            1,
                            expiresUtc).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logWarning($"Failed to update custom threat feed '{trimmedUrl}'", ex);
                    }
                }
            }

            logInformation($"External threat intelligence feed update completed. Ingested/Evaluated {totalIngested} threat IPs.");
        }
        catch (Exception ex)
        {
            logWarning("Error occurred during external threat feed refresh cycle", ex);
        }
        finally
        {
            refreshGate.Release();
        }
    }

    private async Task<int> ProcessSingleFeedAsync(
        string feedName,
        string url,
        ThreatFeedFormat format,
        int minConfidenceOrLevel,
        DateTime expiresUtc)
    {
        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
            using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logWarning($"Threat feed '{feedName}' returned HTTP status {(int)response.StatusCode}", new HttpRequestException($"HTTP {(int)response.StatusCode}"));
                recordAudit?.Invoke("ThreatFeed.Download", "Failed", feedName, $"HTTP {(int)response.StatusCode}");
                return 0;
            }

            string content = await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 8 * 1024 * 1024, cts.Token).ConfigureAwait(false);
            List<string> ips = ThreatFeedParser.ParseFeed(content, format, minConfidenceOrLevel, ThreatFeedParser.DefaultMaxEntriesPerFeed);

            int count = 0;
            foreach (string ip in ips)
            {
                if (EvaluateAndIngest(ip, feedName, expiresUtc))
                {
                    count++;
                }
            }
            recordAudit?.Invoke("ThreatFeed.Download", "Succeeded", feedName, $"Ingested: {count}, Evaluated: {ips.Count}");
            return count;
        }
        catch (Exception ex)
        {
            recordAudit?.Invoke("ThreatFeed.Download", "Failed", feedName, ex.Message);
            throw;
        }
    }

    private async Task<int> ProcessAbuseIpDbFeedAsync(
        string url,
        string apiKey,
        int minConfidence,
        DateTime expiresUtc)
    {
        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("Key", apiKey);
            request.Headers.Add("Accept", "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logWarning($"AbuseIPDB feed returned HTTP status {(int)response.StatusCode}", new HttpRequestException($"HTTP {(int)response.StatusCode}"));
                recordAudit?.Invoke("ThreatFeed.Download", "Failed", "AbuseIPDB Blacklist", $"HTTP {(int)response.StatusCode}");
                return 0;
            }

            string content = await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 8 * 1024 * 1024, cts.Token).ConfigureAwait(false);
            List<string> ips = ThreatFeedParser.ParseFeed(content, ThreatFeedFormat.AbuseIpDbJson, minConfidence, ThreatFeedParser.DefaultMaxEntriesPerFeed);

            int count = 0;
            foreach (string ip in ips)
            {
                if (EvaluateAndIngest(ip, "AbuseIPDB Blacklist", expiresUtc))
                {
                    count++;
                }
            }
            recordAudit?.Invoke("ThreatFeed.Download", "Succeeded", "AbuseIPDB Blacklist", $"Ingested: {count}, Evaluated: {ips.Count}");
            return count;
        }
        catch (Exception ex)
        {
            recordAudit?.Invoke("ThreatFeed.Download", "Failed", "AbuseIPDB Blacklist", ex.Message);
            throw;
        }
    }

    private async Task RefreshDynamicBogonsAsync()
    {
        List<System.Net.IPNetwork> aggregatedNetworks = [];
        bool complete = true;

        // 抓取 IPv4 Fullbogons
        string ipv4Url = config.DynamicBogonIpv4Url;
        if (!string.IsNullOrWhiteSpace(ipv4Url))
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
                using HttpResponseMessage response = await httpClient.GetAsync(ipv4Url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string content = await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 8 * 1024 * 1024, cts.Token).ConfigureAwait(false);
                    var parsed = BogonIpFilter.ParseBogonList(content);
                    if (parsed.Count == 0) complete = false;
                    aggregatedNetworks.AddRange(parsed);
                }
                else
                {
                    complete = false;
                    logWarning($"Dynamic IPv4 Bogon feed returned HTTP status {(int)response.StatusCode}", new HttpRequestException($"HTTP {(int)response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                complete = false;
                logWarning("Failed to download dynamic IPv4 Bogon feed", ex);
            }
        }

        // 抓取 IPv6 Fullbogons
        string ipv6Url = config.DynamicBogonIpv6Url;
        if (!string.IsNullOrWhiteSpace(ipv6Url))
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
                using HttpResponseMessage response = await httpClient.GetAsync(ipv6Url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string content = await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 8 * 1024 * 1024, cts.Token).ConfigureAwait(false);
                    var parsed = BogonIpFilter.ParseBogonList(content);
                    if (parsed.Count == 0) complete = false;
                    aggregatedNetworks.AddRange(parsed);
                }
                else
                {
                    complete = false;
                    logWarning($"Dynamic IPv6 Bogon feed returned HTTP status {(int)response.StatusCode}", new HttpRequestException($"HTTP {(int)response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                complete = false;
                logWarning("Failed to download dynamic IPv6 Bogon feed", ex);
            }
        }

        if (complete && !stopping.IsCancellationRequested && aggregatedNetworks.Count > 0)
        {
            BogonIpFilter.UpdateDynamicBogons(aggregatedNetworks);
            logInformation($"Dynamic Bogon prefix list updated successfully ({aggregatedNetworks.Count} total IPv4/IPv6 prefixes loaded).");
            recordAudit?.Invoke("Bogon.Update", "Succeeded", "Team Cymru Fullbogons", $"{aggregatedNetworks.Count} prefixes updated");
        }
    }

    private bool EvaluateAndIngest(string ip, string sourceFeedName, DateTime expiresUtc)
    {
        stopping.Token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(ip)) return false;

        // 1. Bogon & Reserved 硬過濾
        if (BogonIpFilter.IsBogonOrReserved(ip)) return false;

        // 2. 安全網路白名單（含 DDNS FQDN）過濾
        if (config.UseSafeNetworkList && config.IsInSafeNetwork(ip))
        {
            logInformation($"Threat feed IP '{ip}' matches SafeNetwork whitelist. Safely skipped.");
            return false;
        }

        ThreatIntelligenceItem item = new()
        {
            SourceIp = ip,
            ThreatCategory = "EXTERNAL_FEED",
            ConfidenceScore = 1.0,
            ReportedUtc = DateTime.UtcNow,
            ExpiresUtc = expiresUtc,
            ReporterNodeName = sourceFeedName
        };

        onThreatDiscovered(item);
        return true;
    }

    /// <summary>
    /// 驗證外部自訂情資 URL 是否安全，杜絕連往本機迴路、私有網段或雲端 IMDS 元數據端點之 SSRF 攻擊。
    /// </summary>
    /// <param name="url">待驗證之外部 URL 字串。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>若為安全的外部公網端點傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    internal static async Task<bool> IsSafeExternalUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        string host = uri.DnsSafeHost;
        if (string.IsNullOrWhiteSpace(host))
            return false;

        // 若主機直接為 IP 位址
        if (IPAddress.TryParse(host, out IPAddress? directIp))
        {
            return !BogonIpFilter.IsBogonOrReserved(directIp);
        }

        // 若為網域名稱，解析目標 IP 清單進行 Bogon / 私有網段 / 雲端 IMDS 檢查
        try
        {
            using var dnsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            dnsCts.CancelAfter(TimeSpan.FromSeconds(5));
            IPAddress[] resolved = await Dns.GetHostAddressesAsync(host, dnsCts.Token).ConfigureAwait(false);
            if (resolved.Length == 0)
                return false;

            foreach (IPAddress ip in resolved)
            {
                if (BogonIpFilter.IsBogonOrReserved(ip))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 停止訂閱更新排程。
    /// </summary>
    public void Stop()
    {
        stopping.Cancel();
        refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        refreshTimer?.Dispose();
        refreshTimer = null;
    }

    /// <summary>
    /// 釋放未受控資源。
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Stop();
            try
            {
                if (refreshGate.Wait(TimeSpan.FromSeconds(5)))
                {
                    refreshGate.Release();
                }
            }
            catch { }

            refreshGate.Dispose();
            stopping.Dispose();
            if (ownClient)
            {
                httpClient.Dispose();
            }
        }
    }
}

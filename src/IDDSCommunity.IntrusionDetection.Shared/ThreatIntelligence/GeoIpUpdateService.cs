using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供 GeoIP 國家地理位置資料庫定期自動下載更新、本機離線快取與記憶體熱替換服務。
/// </summary>
public sealed class GeoIpUpdateService : IDisposable
{
    private const string DefaultUserAgent = "IDDSCommunity-GeoIP-Updater/3.0 (+https://github.com/ritai-kgm10179/IDDSCommunity)";
    private readonly IddsConfig config;
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
    /// 初始化 <see cref="GeoIpUpdateService"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域設定執行個體。</param>
    /// <param name="logInformation">資訊日誌回報委派。</param>
    /// <param name="logWarning">警告日誌回報委派。</param>
    /// <param name="httpClient">可選之自訂 HttpClient 執行個體（用於單元測試隔離）。</param>
    /// <param name="recordAudit">可選之稽核日誌回報委派。</param>
    public GeoIpUpdateService(
        IddsConfig config,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null,
        HttpClient? httpClient = null,
        Action<string, string, string, string?>? recordAudit = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
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
            this.httpClient = Network.HttpClientHelper.CreatePooledClient(TimeSpan.FromSeconds(45), userAgent: DefaultUserAgent);
            ownClient = true;
        }
    }

    /// <summary>
    /// 啟動 GeoIP 資料庫背景排程與初始載入作業。
    /// </summary>
    public void Start()
    {
        if (disposed) return;

        // 1. 優先自本機快取或自訂檔案載入既有數據（加速啟動）
        LoadFromLocalOrCache();

        // 2. 啟動定期更新排程（預設每 7 天更新一次）
        int intervalDays = Math.Max(1, config.GeoIpUpdateIntervalDays);
        refreshTimer = new System.Threading.Timer(
            async _ => await RefreshDatabaseAsync(isManual: false).ConfigureAwait(false),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromDays(intervalDays));
    }

    /// <summary>
    /// 停止 GeoIP 背景更新排程。
    /// </summary>
    public void Stop()
    {
        stopping.Cancel();
        refreshTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// 優先自自訂本機檔案路徑或系統預設本機快取檔案載入 GeoIP 數據。
    /// </summary>
    /// <returns>若成功載入數據則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool LoadFromLocalOrCache()
    {
        try
        {
            // 檢查自訂本機檔案（使用串流讀取，杜絕記憶體 LOH 大物件配置）
            string localPath = config.GeoIpLocalFilePath;
            if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
            {
                int loaded = GeoIpLookupService.LoadFromFile(localPath);
                logInformation($"Loaded {loaded} GeoIP records from local file: {localPath}");
                return loaded > 0;
            }

            // 檢查系統預設快取
            string dataDir = IddsConfig.GetDefaultDataDirectory();
            string v4CachePath = Path.Combine(dataDir, "geoip_v4_cache.csv");
            string v6CachePath = Path.Combine(dataDir, "geoip_v6_cache.csv");

            if (File.Exists(v4CachePath) || File.Exists(v6CachePath))
            {
                int loaded = GeoIpLookupService.LoadFromFiles(v4CachePath, v6CachePath);
                logInformation($"Loaded {loaded} GeoIP records from local cache files.");
                return loaded > 0;
            }
        }
        catch (Exception ex)
        {
            logWarning("Failed to load GeoIP database from local file or cache", ex);
        }

        return false;
    }

    /// <summary>
    /// 立即非同步執行一次 GeoIP 資料庫下載、驗證、快取儲存與記憶體熱替換。
    /// </summary>
    /// <param name="isManual">是否為手動強制觸發。</param>
    /// <returns>更新結果物件。</returns>
    public async Task<(bool Success, int TotalRecords, int TotalCountries, string ErrorMessage)> RefreshDatabaseAsync(bool isManual = false)
    {
        if (disposed || stopping.IsCancellationRequested || !await refreshGate.WaitAsync(0).ConfigureAwait(false))
        {
            return (false, GeoIpLookupService.TotalLoadedRecords, GeoIpLookupService.TotalLoadedCountries, "Update is already in progress.");
        }

        try
        {
            if (!isManual && !config.EnableGeoIpAutoUpdate)
            {
                return (false, GeoIpLookupService.TotalLoadedRecords, GeoIpLookupService.TotalLoadedCountries, "Auto-update is disabled.");
            }

            // 若配置了本機檔案，優先以本機檔案為準（串流載入）
            string localPath = config.GeoIpLocalFilePath;
            if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
            {
                int loaded = GeoIpLookupService.LoadFromFile(localPath);
                int countries = GeoIpLookupService.TotalLoadedCountries;
                logInformation($"GeoIP database refreshed from local file: {loaded} records across {countries} countries.");
                return (true, loaded, countries, string.Empty);
            }

            logInformation("Starting GeoIP database download and update cycle...");

            string v4Url = config.GeoIpDatabaseIpv4Url;
            string v6Url = config.GeoIpDatabaseIpv6Url;

            // 並行下載 IPv4 與 IPv6 GeoIP 數據
            Task<string?> v4Task = DownloadGeoIpFeedAsync(v4Url, "IPv4");
            Task<string?> v6Task = DownloadGeoIpFeedAsync(v6Url, "IPv6");

            await Task.WhenAll(v4Task, v6Task).ConfigureAwait(false);

            string? v4Content = await v4Task.ConfigureAwait(false);
            string? v6Content = await v6Task.ConfigureAwait(false);

            if ((!string.IsNullOrWhiteSpace(v4Url) && string.IsNullOrWhiteSpace(v4Content)) ||
                (!string.IsNullOrWhiteSpace(v6Url) && string.IsNullOrWhiteSpace(v6Content)) ||
                (string.IsNullOrWhiteSpace(v4Content) && string.IsNullOrWhiteSpace(v6Content)))
            {
                recordAudit?.Invoke("GeoIp.Update", "Failed", "GeoIP Database", "Failed to download GeoIP feeds from configured URLs.");
                return (false, GeoIpLookupService.TotalLoadedRecords, GeoIpLookupService.TotalLoadedCountries, "Failed to download GeoIP feeds from configured URLs.");
            }

            stopping.Token.ThrowIfCancellationRequested();
            // 熱更新記憶體快取
            int totalLoaded = GeoIpLookupService.LoadValidatedDownload(v4Content, v6Content);
            if (totalLoaded == 0)
            {
                return (false, GeoIpLookupService.TotalLoadedRecords, GeoIpLookupService.TotalLoadedCountries, "Invalid GeoIP feed content.");
            }
            int totalCountries = GeoIpLookupService.TotalLoadedCountries;

            // 儲存至本機快取檔案
            try
            {
                string dataDir = IddsConfig.GetDefaultDataDirectory();
                Directory.CreateDirectory(dataDir);

                if (!string.IsNullOrWhiteSpace(v4Content))
                {
                    string v4CachePath = Path.Combine(dataDir, "geoip_v4_cache.csv");
                    await File.WriteAllTextAsync(v4CachePath, v4Content).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(v6Content))
                {
                    string v6CachePath = Path.Combine(dataDir, "geoip_v6_cache.csv");
                    await File.WriteAllTextAsync(v6CachePath, v6Content).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logWarning("Failed to persist downloaded GeoIP feeds to local cache", ex);
            }

            logInformation($"GeoIP database updated successfully: {totalLoaded} prefixes across {totalCountries} countries loaded.");
            recordAudit?.Invoke("GeoIp.Update", "Succeeded", "GeoIP Database", $"{totalLoaded} prefixes across {totalCountries} countries loaded");
            return (true, totalLoaded, totalCountries, string.Empty);
        }
        catch (Exception ex)
        {
            logWarning("Error occurred during GeoIP database update cycle", ex);
            recordAudit?.Invoke("GeoIp.Update", "Failed", "GeoIP Database", ex.Message);
            return (false, GeoIpLookupService.TotalLoadedRecords, GeoIpLookupService.TotalLoadedCountries, ex.Message);
        }
        finally
        {
            refreshGate.Release();
        }
    }

    private async Task<string?> DownloadGeoIpFeedAsync(string? url, string label)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (Network.NetworkEndpointValidator.IsBlockedImdsOrLinkLocal(url))
        {
            logWarning($"Blocked unsafe or IMDS-suspect {label} GeoIP URL: '{url}'", new InvalidOperationException("IMDS or Link-Local URL blocked"));
            return null;
        }

        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(60));
            using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 64 * 1024 * 1024, cts.Token).ConfigureAwait(false);
            }
            else
            {
                logWarning($"{label} GeoIP feed returned HTTP status {(int)response.StatusCode}", new HttpRequestException($"HTTP {(int)response.StatusCode}"));
                return null;
            }
        }
        catch (Exception ex)
        {
            logWarning($"Failed to download {label} GeoIP database", ex);
            return null;
        }
    }

    /// <summary>
    /// 釋放受控與非受控資源。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        Stop();
        refreshTimer?.Dispose();
        refreshGate.Wait();
        refreshGate.Release();
        if (ownClient)
        {
            httpClient.Dispose();
        }
    }
}

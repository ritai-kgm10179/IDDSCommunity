using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.WAFV2;
using Amazon.WAFV2.Model;
using Amazon.Runtime.CredentialManagement;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 使用官方 AWS SDK、認證鏈與樂觀鎖更新 WAFv2 IP 集合。
/// </summary>
public sealed class AwsPerimeterProvider : ICloudPerimeterProvider, IDisposable
{
    private IAmazonWAFV2? client;
    private readonly bool ownsClient;
    private readonly SemaphoreSlim mutation = new(1, 1);

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.Aws;
    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "AWS WAFv2";
    /// <summary>
    /// 取得或設定 AWS 區域。
    /// </summary>
    public string Region { get; set; } = "us-east-1";
    /// <summary>
    /// 取得或設定 IP 集合識別碼或完整 ARN。
    /// </summary>
    public string IpSetId { get; set; } = string.Empty;
    /// <summary>
    /// 取得或設定 IP 集合名稱；提供 ARN 時自動解析。
    /// </summary>
    public string IpSetName { get; set; } = string.Empty;
    /// <summary>
    /// 取得或設定範圍，支援 REGIONAL 或 CLOUDFRONT。
    /// </summary>
    public string ScopeName { get; set; } = "REGIONAL";
    /// <summary>
    /// 取得或設定 HTTPS 自訂 AWS 端點；一般部署應留空。
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
    /// <summary>
    /// 取得或設定 AWS 認證設定檔名稱；留空使用服務帳戶的官方預設認證鏈。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 建立提供者。
    /// </summary>
    /// <param name="client">可注入的官方 SDK 用戶端；呼叫端負責釋放注入的執行個體。</param>
    public AwsPerimeterProvider(IAmazonWAFV2? client = null)
    {
        this.client = client;
        ownsClient = client is null;
    }

    private IAmazonWAFV2 GetClient()
    {
        if (IpSetId.StartsWith("arn:", StringComparison.Ordinal))
        {
            string[] parts = IpSetId.Split(':', 6);
            string[] resource = parts.Length == 6 ? parts[5].Split('/') : [];
            if (resource.Length != 4 || resource[1] != "ipset") throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Invalid WAFv2 IPSet ARN."));
            ScopeName = resource[0] == "global" ? "CLOUDFRONT" : "REGIONAL";
            Region = ScopeName == "CLOUDFRONT" ? "us-east-1" : parts[3];
            IpSetName = resource[2];
            IpSetId = resource[3];
        }
        if (string.IsNullOrWhiteSpace(IpSetId) || string.IsNullOrWhiteSpace(IpSetName)
            || ScopeName is not ("REGIONAL" or "CLOUDFRONT")) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IPSet name, identifier, and a valid scope are required."));
        if (client is not null) return client;
        AmazonWAFV2Config config = new() { RegionEndpoint = RegionEndpoint.GetBySystemName(ScopeName == "CLOUDFRONT" ? "us-east-1" : Region), Timeout = TimeSpan.FromSeconds(15) };
        if (!string.IsNullOrWhiteSpace(EndpointUrl))
        {
            if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out Uri? endpoint) || endpoint.Scheme != "https")
                throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("AWS endpoint must use HTTPS."));
            config.ServiceURL = endpoint.AbsoluteUri;
        }
        if (string.IsNullOrWhiteSpace(ApiKey)) client = new AmazonWAFV2Client(config);
        else
        {
            if (!new CredentialProfileStoreChain().TryGetAWSCredentials(ApiKey, out var credentials))
                throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("AWS credential profile was not found for the service account."));
            client = new AmazonWAFV2Client(credentials, config);
        }
        return client;
    }

    /// <summary>
    /// 將指定來源加入完整 IP 集合，保留既有位址。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="reason">封鎖原因。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>遠端更新是否已成功接受。</returns>
    public Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default) => UpdateAsync(ipAddress, true, cancellationToken);

    /// <summary>
    /// 僅移除指定來源，保留其他位址。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>遠端更新是否已成功接受。</returns>
    public Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default) => UpdateAsync(ipAddress, false, cancellationToken);

    private async Task<bool> UpdateAsync(string ipAddress, bool block, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress? address)) return false;
        address = IpAddressCanonicalizer.Canonicalize(address);
        string cidr = address + (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "/32" : "/128");
        await mutation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IAmazonWAFV2 sdk = GetClient();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                GetIPSetResponse current = await sdk.GetIPSetAsync(new GetIPSetRequest { Id = IpSetId, Name = IpSetName, Scope = ScopeName }, cancellationToken).ConfigureAwait(false);
                if (current.IPSet is null) return false;
                if ((current.IPSet.IPAddressVersion == Amazon.WAFV2.IPAddressVersion.IPV4) != (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)) return false;
                HashSet<string> addresses = new(current.IPSet.Addresses ?? [], StringComparer.Ordinal);
                bool changed = block ? addresses.Add(cidr) : addresses.Remove(cidr);
                if (!changed) return true;
                try
                {
                    UpdateIPSetResponse result = await sdk.UpdateIPSetAsync(new UpdateIPSetRequest { Id = IpSetId, Name = IpSetName, Scope = ScopeName, LockToken = current.LockToken, Addresses = addresses.Order(StringComparer.Ordinal).ToList(), Description = current.IPSet.Description }, cancellationToken).ConfigureAwait(false);
                    return result.HttpStatusCode == HttpStatusCode.OK;
                }
                catch (WAFOptimisticLockException) when (attempt < 2) { }
            }
            return false;
        }
        finally { mutation.Release(); }
    }

    /// <summary>
    /// 讀取集合以驗證服務帳戶權限。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>連線結果。</returns>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await mutation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IAmazonWAFV2 sdk = GetClient();
            await sdk.GetIPSetAsync(new GetIPSetRequest { Id = IpSetId, Name = IpSetName, Scope = ScopeName }, cancellationToken).ConfigureAwait(false);
            return (true, "已驗證 WAFv2 IPSet 讀取權限。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return (false, ex.GetType().Name); }
        finally { mutation.Release(); }
    }

    /// <summary>
    /// 釋放由提供者建立的 SDK 用戶端。
    /// </summary>
    public void Dispose()
    {
        if (ownsClient) client?.Dispose();
        mutation.Dispose();
    }
}
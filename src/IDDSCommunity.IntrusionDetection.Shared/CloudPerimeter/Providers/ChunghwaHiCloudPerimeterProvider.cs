using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供中華電信 HiCloud / CVPC / CaaS 雲端虛擬私有網路安全群組 (OpenStack Neutron Security Group) 邊界防禦整合。
/// </summary>
public sealed class ChunghwaHiCloudPerimeterProvider : ICloudPerimeterProvider
{


    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.ChunghwaTelecomHiCloud;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "中華電信 HiCloud / CVPC / CaaS (Security Group)";

    /// <summary>
    /// 取得或設定中華電信 CVPC / Neutron API 端點 URL (例如 https://cvpc.hicloud.hinet.net:9696)。
    /// </summary>
    public string EndpointUrl { get; set; } = "https://cvpc.hicloud.hinet.net:9696";

    /// <summary>
    /// 取得或設定中華電信 CVPC 安全群組識別碼 (Security Group ID)。
    /// </summary>
    public string SecurityGroupId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定中華電信 Keystone 認證 Token / API Key (X-Auth-Token)。
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="ChunghwaHiCloudPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public ChunghwaHiCloudPerimeterProvider(HttpClient? httpClient = null)
    {
        // 保留建構子相容性；不建立連線，也不傳送任何規則。
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入中華電信 CVPC 邊界阻絕清單。
    /// </summary>
    public Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    /// <summary>
    /// 拒絕以 Neutron allow 規則模擬解除 deny，避免刪除合法放行規則。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>固定傳回不支援。</returns>
    public Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    /// <summary>
    /// 回報標準 Neutron 安全群組不支援拒絕規則。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>不支援結果及替代配置說明。</returns>
    public Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((false, "Neutron 安全群組僅支援允許規則，無法安全提供封鎖；請設定支援 deny 的防火牆閘道並選擇 GenericWebhook。"));
    }
}
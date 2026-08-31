using System;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

namespace IDDSCommunity.IntrusionDetection.Service.CloudPerimeter;

/// <summary>
/// 負責調度與執行雲端邊界 WAF / NSG / 電信雲防火牆主動聯動之背景服務。
/// </summary>
public sealed class CloudPerimeterService : IDisposable
{
    private readonly CloudPerimeterSettings settings;
    private ICloudPerimeterProvider? activeProvider;
    private bool isDisposed;

    /// <summary>
    /// 取得目前是否已啟用且具備有效提供者。
    /// </summary>
    public bool IsEnabled => settings.EnableCloudPerimeter && activeProvider != null;

    /// <summary>
    /// 取得目前啟用的提供者執行個體。
    /// </summary>
    public ICloudPerimeterProvider? ActiveProvider => activeProvider;

    /// <summary>
    /// 初始化 <see cref="CloudPerimeterService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">雲端邊界整合組態設定。</param>
    public CloudPerimeterService(CloudPerimeterSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        InitializeProvider();
    }

    /// <summary>
    /// 依據目前組態重新初始化提供者。
    /// </summary>
    public void RefreshProvider()
    {
        InitializeProvider();
    }

    private void InitializeProvider()
    {
        if (!settings.EnableCloudPerimeter || settings.ProviderType == CloudPerimeterType.None)
        {
            activeProvider = null;
            return;
        }

        activeProvider = settings.ProviderType switch
        {
            CloudPerimeterType.Aws => new AwsPerimeterProvider
            {
                ApiKey = settings.ApiKey,
                IpSetId = settings.ResourceId,
                Region = string.IsNullOrWhiteSpace(settings.SecondaryId) ? "us-east-1" : settings.SecondaryId,
                EndpointUrl = settings.EndpointUrl
            },
            CloudPerimeterType.Azure => new AzureNsgPerimeterProvider
            {
                BearerToken = settings.ApiKey,
                NetworkSecurityGroupName = settings.ResourceId,
                SubscriptionId = settings.SecondaryId,
                ResourceGroupName = settings.TertiaryId
            },
            CloudPerimeterType.Gcp => new GcpCloudArmorPerimeterProvider
            {
                BearerToken = settings.ApiKey,
                SecurityPolicyName = settings.ResourceId,
                ProjectId = settings.SecondaryId
            },
            CloudPerimeterType.Cloudflare => new CloudflareWafPerimeterProvider
            {
                ApiToken = settings.ApiKey,
                ZoneId = settings.ResourceId
            },
            CloudPerimeterType.ChunghwaTelecomHiCloud => new ChunghwaHiCloudPerimeterProvider
            {
                AuthToken = settings.ApiKey,
                SecurityGroupId = settings.ResourceId,
                EndpointUrl = string.IsNullOrWhiteSpace(settings.EndpointUrl) ? "https://cvpc.hicloud.hinet.net:9696" : settings.EndpointUrl
            },
            CloudPerimeterType.GenericWebhook => new GenericPerimeterWebhookProvider
            {
                WebhookUrl = settings.EndpointUrl,
                AuthHeader = settings.ApiKey
            },
            _ => null
        };
    }

    /// <summary>
    /// 非同步將封鎖 IP 推播至雲端邊界。
    /// </summary>
    /// <param name="ipAddress">封鎖的 IP 位址。</param>
    /// <param name="reason">封鎖原因。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    public async Task<bool> NotifyBlockAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || activeProvider == null) return false;

        try
        {
            return await activeProvider.BlockIpAsync(ipAddress, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[CloudPerimeter] Failed to push block for {ipAddress}: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            return false;
        }
    }

    /// <summary>
    /// 非同步將解除封鎖 IP 推播至雲端邊界。
    /// </summary>
    /// <param name="ipAddress">解除封鎖的 IP 位址。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    public async Task<bool> NotifyUnblockAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || activeProvider == null) return false;

        try
        {
            return await activeProvider.UnblockIpAsync(ipAddress, cancellationToken);
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[CloudPerimeter] Failed to push unblock for {ipAddress}: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            return false;
        }
    }

    /// <summary>
    /// 釋放服務所使用之資源。
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        activeProvider = null;
    }
}

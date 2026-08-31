using System;
using System.Net.Http;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;

/// <summary>
/// 提供依據組態建立對應多雲邊界聯防提供者實例之處理工廠。
/// </summary>
public static class CloudPerimeterProviderFactory
{
    /// <summary>
    /// 依據指定的雲端邊界設定建立對應的 <see cref="ICloudPerimeterProvider"/> 執行個體。
    /// </summary>
    /// <param name="settings">雲端邊界組態設定。</param>
    /// <param name="httpClient">自訂 HTTP 用戶端（選填）。</param>
    /// <returns>傳回對應的雲端邊界提供者，若停用則傳回 <see langword="null"/>。</returns>
    public static ICloudPerimeterProvider? Create(CloudPerimeterSettings settings, HttpClient? httpClient = null)
    {
        if (!settings.EnableCloudPerimeter || settings.ProviderType == CloudPerimeterType.None)
        {
            return null;
        }

        return settings.ProviderType switch
        {
            CloudPerimeterType.Aws => new AwsPerimeterProvider(httpClient),
            CloudPerimeterType.Azure => new AzureNsgPerimeterProvider(httpClient),
            CloudPerimeterType.Gcp => new GcpCloudArmorPerimeterProvider(httpClient),
            CloudPerimeterType.Cloudflare => new CloudflareWafPerimeterProvider(httpClient),
            CloudPerimeterType.ChunghwaTelecomHiCloud => new ChunghwaHiCloudPerimeterProvider(httpClient),
            CloudPerimeterType.GenericWebhook => new GenericPerimeterWebhookProvider(httpClient),
            _ => null
        };
    }
}

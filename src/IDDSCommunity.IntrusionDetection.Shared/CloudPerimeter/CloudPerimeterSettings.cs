using System;
using System.Text.Json.Serialization;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;

/// <summary>
/// 雲端與電信邊界防火牆整合組態設定。
/// </summary>
public sealed class CloudPerimeterSettings
{
    /// <summary>
    /// 取得或設定是否啟用雲端邊界防火牆聯動。
    /// </summary>
    public bool EnableCloudPerimeter { get; set; } = false;

    /// <summary>
    /// 取得或設定雲端邊界提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType { get; set; } = CloudPerimeterType.None;

    /// <summary>
    /// 取得或設定主要 API 存取金鑰 / Token。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定自訂端點 URL / 區域 URL。
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定資源識別碼 (例如 Security Group ID, WAF IPSet ID, Resource Group 等)。
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定次要識別碼 (例如 AWS Region, Azure Subscription ID, GCP Project ID, Cloudflare Zone ID)。
    /// </summary>
    public string SecondaryId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定第三識別碼 (例如 Azure Resource Group 名稱)。
    /// </summary>
    public string TertiaryId { get; set; } = string.Empty;
}

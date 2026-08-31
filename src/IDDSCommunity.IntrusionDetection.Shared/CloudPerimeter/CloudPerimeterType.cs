namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;

/// <summary>
/// 代表支援的雲端與電信邊界防火牆整合提供者類型。
/// </summary>
public enum CloudPerimeterType
{
    /// <summary>
    /// 未啟用或停用。
    /// </summary>
    None = 0,

    /// <summary>
    /// Amazon Web Services (AWS) Security Group / WAFv2 IPSet。
    /// </summary>
    Aws = 1,

    /// <summary>
    /// Microsoft Azure Network Security Group (NSG)。
    /// </summary>
    Azure = 2,

    /// <summary>
    /// Google Cloud Platform (GCP) Cloud Armor / VPC Firewall Rules。
    /// </summary>
    Gcp = 3,

    /// <summary>
    /// Cloudflare WAF IP Access Rules。
    /// </summary>
    Cloudflare = 4,

    /// <summary>
    /// 中華電信 HiCloud / CVPC / CaaS (OpenStack Neutron Security Group)。
    /// </summary>
    ChunghwaTelecomHiCloud = 5,

    /// <summary>
    /// 通用邊界硬體防火牆 Webhook (FortiGate, Palo Alto, OPNsense 等)。
    /// </summary>
    GenericWebhook = 6
}

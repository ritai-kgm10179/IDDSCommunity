using System;
using System.Net;
using System.Net.Sockets;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 提供輸出網路端點與 Webhook URL 之 SSRF 與雲端元數據服務（IMDS）防護檢查工具。
/// </summary>
public static class NetworkEndpointValidator
{
    /// <summary>
    /// 檢查指定的 URL 是否指向雲端執行個體元數據端點（IMDS，例如 169.254.169.254）或 Link-Local 鏈路本地位址。
    /// </summary>
    /// <param name="url">待檢查之端點 URL 字串。</param>
    /// <returns>若為被阻絕之 IMDS 或 Link-Local 端點傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsBlockedImdsOrLinkLocal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        string host = uri.Host;
        if (IPAddress.TryParse(host, out IPAddress? ip))
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] bytes = ip.GetAddressBytes();
                // 169.254.0.0/16 (Link-Local / Cloud IMDS e.g., 169.254.169.254)
                if (bytes[0] == 169 && bytes[1] == 254)
                {
                    return true;
                }
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal)
                {
                    return true;
                }

                // AWS IPv6 IMDS fd00:ec2::254
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 0xfd && bytes[1] == 0x00 && bytes[2] == 0x0e && bytes[3] == 0xc2)
                {
                    return true;
                }
            }
        }
        else if (string.Equals(host, "instance-data", StringComparison.OrdinalIgnoreCase))
        {
            // AWS instance-data hostname
            return true;
        }

        return false;
    }
}
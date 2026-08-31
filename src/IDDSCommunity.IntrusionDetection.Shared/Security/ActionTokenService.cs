using System;
using System.Security.Cryptography;
using System.Text;

namespace IDDSCommunity.IntrusionDetection.Shared.Security;

/// <summary>
/// 提供 SecOps / ChatOps 快速處置動作之防偽安全權杖 (HMAC-SHA256 Action Token) 簽發與驗證服務。
/// </summary>
public static class ActionTokenService
{
    private static readonly byte[] DefaultSecret = SHA256.HashData(Encoding.UTF8.GetBytes("IDDSCommunity_ChatOps_Secret_" + Environment.MachineName));

    /// <summary>
    /// 為指定的處置動作與目標 IP 簽發具備時效性 (TTL) 之防偽安全權杖。
    /// </summary>
    /// <param name="action">處置動作名稱（如 "block" 或 "unblock"）。</param>
    /// <param name="ipAddress">目標來源 IP 位址。</param>
    /// <param name="ttlMinutes">權杖有效時間（分鐘，預設 15 分鐘）。</param>
    /// <param name="secretKey">選擇性自訂密鑰（為空時採用本機環境衍生密鑰）。</param>
    /// <returns>傳回包含動作、IP、到期時間戳與 HMAC 簽署之安全權杖字串。</returns>
    public static string GenerateToken(string action, string ipAddress, int ttlMinutes = 15, string? secretKey = null)
    {
        long expiry = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds();
        string payload = $"{action.ToLowerInvariant()}:{ipAddress}:{expiry}";

        byte[] keyBytes = string.IsNullOrWhiteSpace(secretKey) ? DefaultSecret : SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string signature = Convert.ToHexString(hash).ToLowerInvariant();

        return $"{payload}:{signature}";
    }

    /// <summary>
    /// 驗證安全權杖之有效性、時效性與簽署真實性。
    /// </summary>
    /// <param name="token">待驗證之權杖字串。</param>
    /// <param name="expectedAction">預期的處置動作（如 "block" 或 "unblock"）。</param>
    /// <param name="ipAddress">解析出之目標 IP 位址。</param>
    /// <param name="secretKey">選擇性自訂密鑰。</param>
    /// <returns>若權杖合法且未過期傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool ValidateToken(string? token, string expectedAction, out string ipAddress, string? secretKey = null)
    {
        ipAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;

        string[] parts = token.Split(':');
        if (parts.Length != 4) return false;

        string action = parts[0];
        string ip = parts[1];
        if (!long.TryParse(parts[2], out long expiry)) return false;
        string signature = parts[3];

        if (!string.Equals(action, expectedAction, StringComparison.OrdinalIgnoreCase)) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now > expiry) return false; // 權杖已過期

        string payload = $"{action.ToLowerInvariant()}:{ip}:{expiry}";
        byte[] keyBytes = string.IsNullOrWhiteSpace(secretKey) ? DefaultSecret : SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
        using var hmac = new HMACSHA256(keyBytes);
        byte[] expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string expectedSignature = Convert.ToHexString(expectedHash).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return false;
        }

        ipAddress = ip;
        return true;
    }
}

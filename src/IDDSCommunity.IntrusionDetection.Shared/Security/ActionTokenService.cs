using System;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Globalization;

namespace IDDSCommunity.IntrusionDetection.Shared.Security;

/// <summary>
/// 提供 SecOps / ChatOps 快速處置動作之防偽安全權杖 (HMAC-SHA256 Action Token) 簽發與驗證服務。
/// </summary>
public static class ActionTokenService
{

    /// <summary>
    /// 為指定的處置動作與目標 IP 簽發具備時效性 (TTL) 之防偽安全權杖。
    /// </summary>
    /// <param name="action">處置動作名稱（如 "block" 或 "unblock"）。</param>
    /// <param name="ipAddress">目標來源 IP 位址。</param>
    /// <param name="ttlMinutes">權杖有效時間（分鐘，預設 15 分鐘）。</param>
    /// <param name="secretKey">必要的簽署密鑰，不得為空白。</param>
    /// <returns>傳回包含動作、IP、到期時間戳與 HMAC 簽署之安全權杖字串。</returns>
    public static string GenerateToken(string action, string ipAddress, int ttlMinutes = 15, string? secretKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        if (action is not ("block" or "unblock")) throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unsupported remediation action."), nameof(action));
        if (!IPAddress.TryParse(ipAddress, out IPAddress? address)) throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Invalid target IP address."), nameof(ipAddress));
        if (ttlMinutes is < 1 or > 15) throw new ArgumentOutOfRangeException(nameof(ttlMinutes));
        long expiry = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds();
        string payload = string.Create(CultureInfo.InvariantCulture, $"v2|{action}|{address}|{expiry}");

        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
        byte[] hash;
        try
        {
            using var hmac = new HMACSHA256(keyBytes);
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
        string signature = Convert.ToHexString(hash).ToLowerInvariant();

        return $"{payload}|{signature}";
    }

    /// <summary>
    /// 驗證安全權杖之有效性、時效性與簽署真實性。
    /// </summary>
    /// <param name="token">待驗證之權杖字串。</param>
    /// <param name="expectedAction">預期的處置動作（如 "block" 或 "unblock"）。</param>
    /// <param name="ipAddress">解析出之目標 IP 位址。</param>
    /// <param name="secretKey">必要的簽署密鑰；空白時拒絕驗證。</param>
    /// <returns>若權杖合法且未過期傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool ValidateToken(string? token, string expectedAction, out string ipAddress, string? secretKey = null)
    {
        ipAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512 || string.IsNullOrWhiteSpace(secretKey)) return false;

        string[] parts = token.Split('|');
        if (parts.Length != 5 || parts[0] != "v2") return false;

        string action = parts[1];
        string ip = parts[2];
        if (!IPAddress.TryParse(ip, out _) || action is not ("block" or "unblock")) return false;
        if (!long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long expiry)) return false;
        string signature = parts[4];

        if (!string.Equals(action, expectedAction, StringComparison.OrdinalIgnoreCase)) return false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now >= expiry || expiry - now > 900) return false;

        string payload = string.Join('|', parts.AsSpan(0, 4).ToArray());
        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
        byte[] expectedHash;
        try
        {
            using var hmac = new HMACSHA256(keyBytes);
            expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
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

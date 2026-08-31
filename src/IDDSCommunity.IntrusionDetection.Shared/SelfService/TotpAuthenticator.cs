using System;
using System.Security.Cryptography;
using System.Text;

namespace IDDSCommunity.IntrusionDetection.Shared.SelfService;

/// <summary>
/// 提供符合 RFC 6238 (TOTP) 與 RFC 4226 (HOTP) 標準之時間型一次性動態密碼產生與驗證演算法。
/// </summary>
public static class TotpAuthenticator
{
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// 產生一組密碼學安全隨機的 160 位元 Base32 密鑰字串。
    /// </summary>
    /// <returns>傳回 Base32 編碼之密鑰。</returns>
    public static string GenerateSecretKey()
    {
        byte[] buffer = new byte[20];
        RandomNumberGenerator.Fill(buffer);
        return ToBase32String(buffer);
    }

    /// <summary>
    /// 產生符合標準 authenticator URI (otpauth://) 之文字字串，便利繪製 QR Code。
    /// </summary>
    /// <param name="issuer">發行者標籤 (例如 IDDS Community)。</param>
    /// <param name="accountTitle">帳戶或主機識別標題。</param>
    /// <param name="secretKey">Base32 密鑰。</param>
    /// <returns>傳回 otpauth URI。</returns>
    public static string GenerateOtpAuthUri(string issuer, string accountTitle, string secretKey)
    {
        string encodedIssuer = Uri.EscapeDataString(issuer);
        string encodedAccount = Uri.EscapeDataString(accountTitle);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    /// <summary>
    /// 依據指定 Base32 密鑰與時間點計算出 6 位數 TOTP 動態密碼。
    /// </summary>
    /// <param name="secretKey">Base32 密鑰。</param>
    /// <param name="time">計算基準時間 (預設為 UtcNow)。</param>
    /// <param name="timeStepSeconds">時間間隔秒數 (預設 30 秒)。</param>
    /// <returns>傳回 6 位數字串。</returns>
    public static string GenerateCode(string secretKey, DateTime? time = null, int timeStepSeconds = 30)
    {
        byte[] keyBytes = FromBase32String(secretKey);
        long unixTime = (long)(time ?? DateTime.UtcNow).Subtract(DateTime.UnixEpoch).TotalSeconds;
        long timeStep = unixTime / timeStepSeconds;

        byte[] timeStepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeStepBytes);

        using var hmac = new HMACSHA1(keyBytes);
        byte[] hash = hmac.ComputeHash(timeStepBytes);

        int offset = hash[^1] & 0x0F;
        int binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

        int otp = binaryCode % 1_000_000;
        return otp.ToString("D6");
    }

    /// <summary>
    /// 驗證使用者輸入之 6 位數動態密碼是否有效 (允許前後 1 個時間窗口容許時鐘偏移)。
    /// </summary>
    /// <param name="secretKey">Base32 密鑰。</param>
    /// <param name="code">使用者輸入之 6 位數代碼。</param>
    /// <param name="time">驗證基準時間 (預設為 UtcNow)。</param>
    /// <param name="timeStepSeconds">時間間隔秒數 (預設 30 秒)。</param>
    /// <param name="allowedDriftSteps">允許容許之時鐘偏移步數 (預設 1)。</param>
    /// <returns>若代碼驗證相符則傳回 true，否則傳回 false。</returns>
    public static bool VerifyCode(string secretKey, string code, DateTime? time = null, int timeStepSeconds = 30, int allowedDriftSteps = 1)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        if (code.Length != 6) return false;

        DateTime now = time ?? DateTime.UtcNow;
        for (int i = -allowedDriftSteps; i <= allowedDriftSteps; i++)
        {
            DateTime checkTime = now.AddSeconds(i * timeStepSeconds);
            string expected = GenerateCode(secretKey, checkTime, timeStepSeconds);
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(code)))
                return true;
        }

        return false;
    }

    private static string ToBase32String(byte[] data)
    {
        if (data.Length == 0) return string.Empty;
        StringBuilder sb = new();
        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;
        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }
            int index = 0x1F & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            sb.Append(Base32Chars[index]);
        }
        return sb.ToString();
    }

    private static byte[] FromBase32String(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32)) return [];
        base32 = base32.Trim().TrimEnd('=').ToUpperInvariant();
        byte[] output = new byte[base32.Length * 5 / 8];
        int bitBuffer = 0;
        int currentBits = 0;
        int outIndex = 0;

        foreach (char c in base32)
        {
            int charVal = Base32Chars.IndexOf(c);
            if (charVal < 0) continue;

            bitBuffer = (bitBuffer << 5) | charVal;
            currentBits += 5;

            if (currentBits >= 8)
            {
                currentBits -= 8;
                if (outIndex < output.Length)
                    output[outIndex++] = (byte)(bitBuffer >> currentBits);
            }
        }

        return output[..outIndex];
    }
}

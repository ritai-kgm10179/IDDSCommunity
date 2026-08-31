using System;
using IDDSCommunity.IntrusionDetection.Shared.SelfService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 TotpAuthenticator 產生之 Base32 密鑰、RFC 6238 TOTP 計算與時間動態驗證邏輯。
/// </summary>
[TestClass]
public sealed class TotpAuthenticatorTest
{
    /// <summary>
    /// 驗證 Base32 密鑰產生具備標準長度與字符集。
    /// </summary>
    [TestMethod]
    public void GenerateSecretKey_ReturnsValidBase32String()
    {
        string secret = TotpAuthenticator.GenerateSecretKey();
        Assert.IsNotNull(secret);
        Assert.AreEqual(32, secret.Length);
        foreach (char c in secret)
        {
            Assert.IsTrue((c >= 'A' && c <= 'Z') || (c >= '2' && c <= '7'), $"Invalid Base32 character: {c}");
        }
    }

    /// <summary>
    /// 驗證 GenerateOtpAuthUri 產生正確的 otpauth URI。
    /// </summary>
    [TestMethod]
    public void GenerateOtpAuthUri_FormatsCorrectly()
    {
        string uri = TotpAuthenticator.GenerateOtpAuthUri("IDDS Community", "Server01", "JBSWY3DPEHPK3PXP");
        Assert.IsTrue(uri.StartsWith("otpauth://totp/IDDS%20Community:Server01", StringComparison.Ordinal));
        Assert.IsTrue(uri.Contains("secret=JBSWY3DPEHPK3PXP", StringComparison.Ordinal));
        Assert.IsTrue(uri.Contains("issuer=IDDS%20Community", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 TOTP 代碼於相同時間點計算相符且驗證成功。
    /// </summary>
    [TestMethod]
    public void GenerateAndVerifyCode_MatchesAndSucceeds()
    {
        string secret = "JBSWY3DPEHPK3PXP";
        DateTime fixedTime = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        string code = TotpAuthenticator.GenerateCode(secret, fixedTime);
        Assert.IsNotNull(code);
        Assert.AreEqual(6, code.Length);

        bool isValid = TotpAuthenticator.VerifyCode(secret, code, fixedTime);
        Assert.IsTrue(isValid, "Code generated at the same timestamp must be valid.");

        // 驗證時間容許偏移 (前後 30 秒)
        bool isValidDriftForward = TotpAuthenticator.VerifyCode(secret, code, fixedTime.AddSeconds(20));
        Assert.IsTrue(isValidDriftForward, "Code within 30-second drift should be valid.");

        // 驗證超時過期代碼 (超過 60 秒)
        bool isExpired = TotpAuthenticator.VerifyCode(secret, code, fixedTime.AddSeconds(120));
        Assert.IsFalse(isExpired, "Expired code must be rejected.");

        // 驗證錯誤代碼
        bool isInvalidCode = TotpAuthenticator.VerifyCode(secret, "000000", fixedTime);
        Assert.IsFalse(isInvalidCode, "Wrong code must be rejected.");
    }
}

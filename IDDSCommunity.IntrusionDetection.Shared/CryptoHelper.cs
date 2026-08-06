using System;
using System.Text;
using System.Security.Cryptography;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal class CryptoHelper
{
    private const string CurrentPrefix = "dpapi:v1:";
    private const string YYHAU_SDBN = "usHN,:_ADs24adH:S";
    private static readonly byte[] OptionalEntropy = SHA256.HashData(Encoding.UTF8.GetBytes("IDDSCommunity.IntrusionDetection.SmtpPassword.v1"));

    /// <summary>
    /// Determines whether encrypted text uses the current protected-data format.
    /// </summary>
    /// <param name="cipherText">The encrypted text.</param>
    /// <returns><see langword="true"/> when the current format prefix is present.</returns>
    internal static bool IsCurrentFormat(string cipherText) => cipherText.StartsWith(CurrentPrefix, StringComparison.Ordinal);

    /// <summary>
    /// 執行encrypt作業。
    /// </summary>
    /// <param name="toEncrypt">to encrypt參數。</param>
    /// <param name="useHashing">use hashing參數。</param>
    /// <returns>傳回encrypt結果。</returns>

    internal static string Encrypt(string toEncrypt, bool useHashing)
    {
        ArgumentNullException.ThrowIfNull(toEncrypt);
        byte[] clearText = Encoding.UTF8.GetBytes(toEncrypt);
        try
        {
            byte[] protectedData = ProtectedData.Protect(clearText, OptionalEntropy, DataProtectionScope.LocalMachine);
            return CurrentPrefix + Convert.ToBase64String(protectedData);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clearText);
        }
    }

    /// <summary>
    /// 執行decrypt作業。
    /// </summary>
    /// <param name="cipherString">cipher string參數。</param>
    /// <param name="useHashing">use hashing參數。</param>
    /// <returns>傳回decrypt結果。</returns>

    internal static string Decrypt(string cipherString, bool useHashing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherString);
        if (!IsCurrentFormat(cipherString))
        {
            return LegacyDecrypt(cipherString, useHashing);
        }

        byte[] protectedData = Convert.FromBase64String(cipherString[CurrentPrefix.Length..]);
        byte[] clearText = ProtectedData.Unprotect(protectedData, OptionalEntropy, DataProtectionScope.LocalMachine);
        try
        {
            return Encoding.UTF8.GetString(clearText);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clearText);
        }
    }

    /// <summary>
    /// Decrypts a legacy TripleDES value solely for one-time migration to protected storage.
    /// </summary>
    /// <param name="cipherString">legacy encrypted參數。</param>
    /// <param name="useHashing">Whether the legacy key used MD5 derivation.</param>
    /// <returns>The decrypted legacy value.</returns>
    private static string LegacyDecrypt(string cipherString, bool useHashing)
    {
        byte[] keyArray;
        byte[] toEncryptArray = Convert.FromBase64String(cipherString);
        string key = YYHAU_SDBN;

        if (useHashing)
        {
            using var hashmd5 = MD5.Create();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
        else
        {
            keyArray = Encoding.UTF8.GetBytes(key);
        }

        using var tdes = TripleDES.Create();
        tdes.Key = keyArray;
        tdes.Mode = CipherMode.ECB;
        tdes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform cTransform = tdes.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Encoding.UTF8.GetString(resultArray);
    }
}

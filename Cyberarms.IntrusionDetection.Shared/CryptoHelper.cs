using System;
using System.Text;
using System.Security.Cryptography;

namespace Cyberarms.IntrusionDetection.Shared;

internal class CryptoHelper
{
    private const string CurrentPrefix = "dpapi:v1:";
    private const string YYHAU_SDBN = "usHN,:_ADs24adH:S";
    private static readonly byte[] OptionalEntropy = SHA256.HashData(Encoding.UTF8.GetBytes("Cyberarms.IntrusionDetection.SmtpPassword.v1"));

    internal static bool IsCurrentFormat(string cipherText) => cipherText.StartsWith(CurrentPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Executes the encrypt operation.
    /// </summary>
    /// <param name="toEncrypt">The to encrypt value.</param>
    /// <param name="useHashing">The use hashing value.</param>
    /// <returns>The encrypt result.</returns>

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
    /// Executes the decrypt operation.
    /// </summary>
    /// <param name="cipherString">The cipher string value.</param>
    /// <param name="useHashing">The use hashing value.</param>
    /// <returns>The decrypt result.</returns>

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

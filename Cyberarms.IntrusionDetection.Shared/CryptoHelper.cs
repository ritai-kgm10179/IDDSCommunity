using System;
using System.Text;
using System.Security.Cryptography;

namespace Cyberarms.IntrusionDetection.Shared;

internal class CryptoHelper
{
    private const string YYHAU_SDBN = "usHN,:_ADs24adH:S";

    /// <summary>
    /// Executes the encrypt operation.
    /// </summary>
    /// <param name="toEncrypt">The to encrypt value.</param>
    /// <param name="useHashing">The use hashing value.</param>
    /// <returns>The encrypt result.</returns>

    internal static string Encrypt(string toEncrypt, bool useHashing)
    {
        byte[] keyArray;
        byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);
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

        using ICryptoTransform cTransform = tdes.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    /// <summary>
    /// Executes the decrypt operation.
    /// </summary>
    /// <param name="cipherString">The cipher string value.</param>
    /// <param name="useHashing">The use hashing value.</param>
    /// <returns>The decrypt result.</returns>

    internal static string Decrypt(string cipherString, bool useHashing)
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

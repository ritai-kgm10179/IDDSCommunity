using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 管理由 Windows 資料保護 API 保護的 SQLite 資料庫金鑰。
/// </summary>
internal static class DatabaseEncryptionKeyStore
{
    private const int KeySize = 32;
    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("IDDSCommunity.SQLite.DatabaseKey.v1");

    /// <summary>
    /// 取得現有金鑰，或以不可預測的隨機值建立受保護的新金鑰。
    /// </summary>
    /// <param name="databasePath">SQLite 資料庫的完整路徑。</param>
    /// <param name="allowCreate">指出不存在金鑰時是否允許建立。</param>
    /// <returns>可供 SQLite3MultipleCiphers 使用的 Base64 密碼。</returns>
    public static string GetPassword(string databasePath, bool allowCreate)
    {
        string keyPath = GetKeyPath(databasePath);
        if (!File.Exists(keyPath))
        {
            if (!allowCreate)
                throw new InvalidDataException(Localization.Strings.Get("The encrypted database key is missing. Database access was refused to prevent data loss."));
            CreateProtectedKey(keyPath);
        }

        HardenAccessControl(keyPath);
        byte[] protectedKey = File.ReadAllBytes(keyPath);
        byte[] key = ProtectedData.Unprotect(protectedKey, OptionalEntropy, DataProtectionScope.LocalMachine);
        try
        {
            if (key.Length != KeySize)
                throw new InvalidDataException(Localization.Strings.Get("The encrypted database key has an invalid length."));
            return Convert.ToBase64String(key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// 取得資料庫金鑰檔案的完整路徑。
    /// </summary>
    /// <param name="databasePath">SQLite 資料庫的完整路徑。</param>
    /// <returns>金鑰檔案路徑。</returns>
    public static string GetKeyPath(string databasePath) => Path.GetFullPath(databasePath) + ".key";

    private static void CreateProtectedKey(string keyPath)
    {
        byte[] key = RandomNumberGenerator.GetBytes(KeySize);
        byte[] protectedKey = ProtectedData.Protect(key, OptionalEntropy, DataProtectionScope.LocalMachine);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
            try
            {
                using FileStream stream = new(keyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
                stream.Write(protectedKey);
                stream.Flush(true);
                HardenAccessControl(keyPath);
            }
            catch (IOException) when (File.Exists(keyPath))
            {
                // 另一個處理程序已完成原子建立，直接使用其金鑰。
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(protectedKey);
        }
    }

    private static void HardenAccessControl(string keyPath)
    {
        string commonApplicationData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        if (!Path.GetFullPath(keyPath).StartsWith(commonApplicationData + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        FileSecurity security = new();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl,
            AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl,
            AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
            FileSystemRights.Read,
            AccessControlType.Allow));
        FileSystemAclExtensions.SetAccessControl(new FileInfo(keyPath), security);
    }
}

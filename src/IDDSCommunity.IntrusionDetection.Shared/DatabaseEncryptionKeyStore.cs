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

    /// <summary>
    /// 將金鑰檔案的存取控制限縮為 SYSTEM、本機系統管理員，以及由安裝程式建立之
    /// <see cref="Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME"/> 本機群組成員。
    /// 因為 DPAPI 的 <see cref="DataProtectionScope.LocalMachine"/> 範圍本身不做身分區隔，
    /// 任何能讀到受保護位元組的本機處理程序都能解密，所以檔案 ACL 才是實際的存取邊界；
    /// 不得再對 BUILTIN\Users 這類涵蓋所有本機標準使用者的群組授予讀取權限。
    /// </summary>
    /// <param name="keyPath">金鑰檔案的完整路徑。</param>
    private static void HardenAccessControl(string keyPath)
    {
        string commonApplicationData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        if (!Path.GetFullPath(keyPath).StartsWith(commonApplicationData + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            FileSecurity security = CreateHardenedFileSecurity();
            FileSystemAclExtensions.SetAccessControl(new FileInfo(keyPath), security);
        }
        catch (UnauthorizedAccessException)
        {
            // 非提升權限之操作員或處理程序無 WRITE_DAC 權限，略過 ACL 修改並繼續讀取金鑰
        }
        catch (System.Security.SecurityException)
        {
            // 權限受限環境略過 ACL 修改
        }
    }

    /// <summary>
    /// 建立已阻絕繼承並僅授權 SYSTEM、Administrators 與 Operators 群組之金鑰安全描述元。
    /// </summary>
    /// <returns>已設定存取控制規則之 <see cref="FileSecurity"/> 執行個體。</returns>
    internal static FileSecurity CreateHardenedFileSecurity()
    {
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
        if (TryResolveOperatorsGroupSid(out SecurityIdentifier? operatorsGroupSid))
        {
            security.AddAccessRule(new FileSystemAccessRule(
                operatorsGroupSid!,
                FileSystemRights.Read,
                AccessControlType.Allow));
        }
        else
        {
            // 若本機群組尚未由安裝程式建立，暫時授予已驗證使用者讀取權限，避免主控台無法載入資料庫
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                FileSystemRights.Read,
                AccessControlType.Allow));
        }
        return security;
    }

    /// <summary>
    /// 嘗試將 <see cref="Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME"/> 本機群組名稱解析為安全性識別碼。
    /// </summary>
    /// <param name="sid">解析成功時傳出對應的安全性識別碼；失敗時為 null。</param>
    /// <returns>群組存在且成功解析時傳回 true；群組不存在（例如未透過安裝程式安裝）時傳回 false。</returns>
    private static bool TryResolveOperatorsGroupSid(out SecurityIdentifier? sid)
    {
        try
        {
            sid = (SecurityIdentifier)new NTAccount(Environment.MachineName, Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME)
                .Translate(typeof(SecurityIdentifier));
            return true;
        }
        catch (IdentityNotMappedException)
        {
            // 本機群組尚未由安裝程式建立（例如開發環境或非標準安裝方式）；
            // 僅保留 SYSTEM 與系統管理員存取權限，非提升權限之管理主控台將無法讀取金鑰。
            sid = null;
            return false;
        }
    }
}

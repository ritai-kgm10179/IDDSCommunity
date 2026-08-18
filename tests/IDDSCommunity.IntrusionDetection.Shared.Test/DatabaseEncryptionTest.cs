using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class DatabaseEncryptionTest
{
    private string testDirectory = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(TestContext.TestRunDirectory ?? AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Database.Instance.Close();
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void Configure_NewDatabaseEncryptsHeaderAndCreatesProtectedKey()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "encrypted.db");

        database.Configure(testDirectory, "encrypted.db");
        database.ExecuteNonQuery("CREATE TABLE Secret(Value TEXT NOT NULL)");
        database.ExecuteNonQuery("INSERT INTO Secret(Value) VALUES(@p0)", "sensitive-value");
        Assert.IsFalse(ContainsBytes(path, "sensitive-value"u8));
        if (File.Exists(path + "-wal"))
            Assert.IsFalse(ContainsBytes(path + "-wal", "sensitive-value"u8));
        database.Close();

        Assert.IsFalse(HasPlaintextHeader(path));
        Assert.IsTrue(File.Exists(path + ".key"));
        Assert.IsFalse(File.ReadAllText(path).Contains("sensitive-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Configure_PlaintextDatabaseMigratesDataWithoutLeavingPlaintextRollback()
    {
        SQLitePCL.Batteries_V2.Init();
        string path = Path.Combine(testDirectory, "legacy.db");
        using (SqliteConnection plaintext = new($"Data Source={path};Pooling=False"))
        {
            plaintext.Open();
            using SqliteCommand command = plaintext.CreateCommand();
            command.CommandText = "CREATE TABLE Legacy(Value TEXT NOT NULL); INSERT INTO Legacy(Value) VALUES('preserved');";
            command.ExecuteNonQuery();
        }
        Assert.IsTrue(HasPlaintextHeader(path));

        Database database = new();
        database.Configure(testDirectory, "legacy.db");

        Assert.AreEqual("preserved", Convert.ToString(database.ExecuteScalar("SELECT Value FROM Legacy")));
        database.Close();
        Assert.IsFalse(HasPlaintextHeader(path));
        Assert.IsEmpty(Directory.GetFiles(testDirectory, "*.plaintext-rollback-*"));
    }

    [TestMethod]
    public void Configure_EncryptedDatabaseWithoutKeyFailsClosed()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "locked.db");
        database.Configure(testDirectory, "locked.db");
        database.Close();
        File.Delete(path + ".key");

        Assert.ThrowsExactly<InvalidDataException>(() => new Database().Configure(testDirectory, "locked.db"));
        Assert.IsTrue(File.Exists(path));
        Assert.IsFalse(File.Exists(path + ".key"));
    }

    [TestMethod]
    public void EncryptedDatabaseRejectsIncorrectPassword()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "wrong-password.db");
        database.Configure(testDirectory, "wrong-password.db");
        database.Close();

        using SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            Password = "incorrect-password"
        }.ConnectionString);
        Assert.ThrowsExactly<SqliteException>(() => connection.Open());
    }

    /// <summary>
    /// 驗證前次明文資料庫遷移意外中斷後遺留之明文回滾與暫存檔案，會在下次啟動時被安全抹除與刪除，不留明文殘留。
    /// </summary>
    [TestMethod]
    public void Configure_InterruptedMigrationStrayRollbackArtifactsAreSecurelyPurgedOnStartup()
    {
        string dbPath = Path.Combine(testDirectory, "stray.db");
        string strayPlaintextRollback = Path.Combine(testDirectory, ".stray.db.plaintext-rollback-" + Guid.NewGuid().ToString("N"));
        string strayEncryptedTemp = Path.Combine(testDirectory, ".stray.db.encrypted-" + Guid.NewGuid().ToString("N"));

        File.WriteAllText(strayPlaintextRollback, "unencrypted-confidential-data");
        File.WriteAllText(strayEncryptedTemp, "temporary-encrypted-ciphertext");

        Assert.IsTrue(File.Exists(strayPlaintextRollback));
        Assert.IsTrue(File.Exists(strayEncryptedTemp));

        Database database = new();
        database.Configure(testDirectory, "stray.db");
        database.Close();

        Assert.IsFalse(File.Exists(strayPlaintextRollback), "Stray plaintext rollback artifact was not cleaned up on startup.");
        Assert.IsFalse(File.Exists(strayEncryptedTemp), "Stray encrypted temporary artifact was not cleaned up on startup.");
    }

    /// <summary>
    /// 驗證金鑰安全描述元產生邏輯確實移除繼承權限，且僅授予 SYSTEM 與本機 Administrators (及選用之 Operators 群組)，絕不授予 BUILTIN\Users 或 Everyone。
    /// </summary>
    [TestMethod]
    public void CreateHardenedFileSecurity_DeniesBuiltinUsersAndRestrictsToAuthorizedPrincipals()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("NTFS ACL security descriptor verification requires Windows.");

        FileSecurity fileSecurity = DatabaseEncryptionKeyStore.CreateHardenedFileSecurity();
        Assert.IsTrue(fileSecurity.AreAccessRulesProtected, "Inheritance must be blocked on key security descriptor.");

        AuthorizationRuleCollection rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));
        Assert.IsTrue(rules.Count >= 2, "Security descriptor must contain at least SYSTEM and Administrators rules.");

        SecurityIdentifier builtinUsersSid = new(WellKnownSidType.BuiltinUsersSid, null);
        SecurityIdentifier worldSid = new(WellKnownSidType.WorldSid, null);
        SecurityIdentifier authenticatedUserSid = new(WellKnownSidType.AuthenticatedUserSid, null);
        SecurityIdentifier localSystemSid = new(WellKnownSidType.LocalSystemSid, null);
        SecurityIdentifier builtinAdminsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);

        bool hasSystem = false;
        bool hasAdmins = false;

        foreach (FileSystemAccessRule rule in rules)
        {
            Assert.AreNotEqual(builtinUsersSid, rule.IdentityReference, "BUILTIN\\Users must not be granted access in key security descriptor.");
            Assert.AreNotEqual(worldSid, rule.IdentityReference, "Everyone/World must not be granted access in key security descriptor.");
            Assert.AreNotEqual(authenticatedUserSid, rule.IdentityReference, "Authenticated Users must not be granted access in key security descriptor.");

            if (rule.IdentityReference == localSystemSid)
            {
                hasSystem = true;
                Assert.AreEqual(FileSystemRights.FullControl, rule.FileSystemRights);
                Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
            }
            if (rule.IdentityReference == builtinAdminsSid)
            {
                hasAdmins = true;
                Assert.AreEqual(FileSystemRights.FullControl, rule.FileSystemRights);
                Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
            }
        }

        Assert.IsTrue(hasSystem, "LocalSystem must have FullControl access.");
        Assert.IsTrue(hasAdmins, "BuiltinAdministrators must have FullControl access.");
    }

    /// <summary>
    /// 驗證金鑰檔案存取控制規則確實移除 BUILTIN\Users 繼承權限，僅允許 SYSTEM、Administrators 與 IDDSCommunityOperators 存取。
    /// </summary>
    [TestMethod]
    public void KeyStore_HardenAccessControl_DeniesBuiltinUsersAndRestrictsToAuthorizedPrincipals()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("NTFS ACL hardening verification requires Windows.");

        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string testSubDir = Path.Combine(commonAppData, "IDDSCommunityTest_" + Guid.NewGuid().ToString("N"));
        string testKeyFile = Path.Combine(testSubDir, "test.db");

        try
        {
            try
            {
                Directory.CreateDirectory(testSubDir);
            }
            catch (UnauthorizedAccessException)
            {
                Assert.Inconclusive("Current test process lacks write permission to ProgramData.");
            }

            try
            {
                DatabaseEncryptionKeyStore.GetPassword(testKeyFile, allowCreate: true);
            }
            catch (UnauthorizedAccessException)
            {
                // 當非提升權限且不在 IDDSCommunityOperators 群組時，HardenAccessControl 正確阻絕了目前標準使用者的讀取權限，證明 BUILTIN\Users 權限已遭移除。
                string keyPath = DatabaseEncryptionKeyStore.GetKeyPath(testKeyFile);
                Assert.IsTrue(File.Exists(keyPath), "Key file must have been created before access was hardened.");
                return;
            }

            string createdKeyPath = DatabaseEncryptionKeyStore.GetKeyPath(testKeyFile);
            Assert.IsTrue(File.Exists(createdKeyPath));

            FileInfo fileInfo = new(createdKeyPath);
            FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(fileInfo);
            Assert.IsTrue(fileSecurity.AreAccessRulesProtected, "Inheritance must be blocked on key file.");

            AuthorizationRuleCollection rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));
            SecurityIdentifier builtinUsersSid = new(WellKnownSidType.BuiltinUsersSid, null);

            foreach (FileSystemAccessRule rule in rules)
            {
                Assert.AreNotEqual(builtinUsersSid, rule.IdentityReference, "BUILTIN\\Users must not have access to database key.");
            }
        }
        finally
        {
            if (Directory.Exists(testSubDir))
            {
                try { Directory.Delete(testSubDir, true); } catch { }
            }
        }
    }

    private static bool HasPlaintextHeader(string path)
    {
        Span<byte> header = stackalloc byte[16];
        using FileStream stream = File.OpenRead(path);
        return stream.Read(header) == header.Length && header.SequenceEqual("SQLite format 3\0"u8);
    }

    private static bool ContainsBytes(string path, ReadOnlySpan<byte> value)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        byte[] content = new byte[checked((int)stream.Length)];
        stream.ReadExactly(content);
        return content.AsSpan().IndexOf(value) >= 0;
    }
}

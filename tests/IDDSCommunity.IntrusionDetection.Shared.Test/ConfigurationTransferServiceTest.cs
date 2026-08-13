using System;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class ConfigurationTransferServiceTest
{
    private string testDirectory = null!;
    private Database database = null!;
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(TestContext.TestRunDirectory ?? AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "transfer.db");
        IddsConfig configuration = IddsConfig.GetDefaultConfiguration();
        configuration.ApplicationPath = testDirectory;
        configuration.HardLockAttempts = 20;
        configuration.SoftLockAttempts = 10;
        configuration.SmtpPort = 587;
        configuration.SmtpServer = "smtp.example.test";
        configuration.SmtpUsername = "not-secret";
        configuration.SmtpPassword = CryptoHelper.Encrypt("portable-secret", true);
        SaveConfiguration(configuration);
        database.ExecuteNonQuery("INSERT INTO AppConfig(ConfigKey,ConfigValue) VALUES(@p0,@p1)", "Configuration.Language", "zh-TW");
        database.ExecuteNonQuery("INSERT INTO AppConfig(ConfigKey,ConfigValue) VALUES(@p0,@p1)", IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE, FirewallBlockMode.Bidirectional.ToString());
        database.ExecuteNonQuery("INSERT INTO WhiteList(IpAddress,NetworkMask) VALUES(@p0,@p1)", "192.0.2.0", "255.255.255.0");
        database.ExecuteNonQuery("INSERT INTO SecurityAgents(AgentId,Name,AssemblyName,HardLockAttempts,HardLockTimeHours,LockForever,SoftLockAttempts,SoftLockTimeMinutes,OverwriteConfiguration,DisplayName,Enabled,Serial) VALUES(@p0,@p1,@p2,20,1,0,10,1,0,@p3,1,0)", Guid.Parse("fa68919b-6d0b-4508-9659-3cd1e160235c"), "OpenSshSecurityAgent", "IDDSCommunity.Agents.OpenSsh.dll", "OpenSSH");
    }

    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void ExportOmitsSecretsAndMachineSpecificPathsByDefault()
    {
        ConfigurationTransferPackage package = new ConfigurationTransferService(database).Export();
        string json = System.Text.Json.JsonSerializer.Serialize(package);
        Assert.IsNull(package.Secrets);
        Assert.IsFalse(json.Contains("portable-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(testDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.HasCount(1, package.SafeNetworks);
        Assert.HasCount(1, package.Agents);
        Assert.AreEqual("Bidirectional", package.ApplicationSettings[IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE]);
    }

    /// <summary>
    /// 驗證舊資料庫的空白防火牆模式會依執行階段規則正規化為輸入封鎖。
    /// </summary>
    [TestMethod]
    public void ExportNormalizesBlankFirewallModeToInbound()
    {
        database.ExecuteNonQuery(
            "UPDATE AppConfig SET ConfigValue=@p0 WHERE ConfigKey=@p1",
            string.Empty,
            IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE);

        ConfigurationTransferPackage package = new ConfigurationTransferService(database).Export();

        Assert.AreEqual(
            FirewallBlockMode.Inbound.ToString(),
            package.ApplicationSettings[IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE]);
    }

    [TestMethod]
    public void ImportRejectsUnsupportedFirewallMode()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "settings.json");
        service.ExportToFile(path);
        ConfigurationTransferPackage package = service.ReadPackage(path);
        package.ApplicationSettings[IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE] = "BlackholeRouting";
        File.WriteAllText(path, JsonSerializer.Serialize(package));

        Assert.ThrowsExactly<InvalidDataException>(() => service.ReadPackage(path));
    }

    /// <summary>
    /// 驗證設定套件錯誤會指出實際欄位和值。
    /// </summary>
    [TestMethod]
    public void ValidationFailureIdentifiesInvalidFieldAndValue()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "invalid-settings.json");
        service.ExportToFile(path);
        ConfigurationTransferPackage package = service.ReadPackage(path);
        package.GlobalPolicy.SmtpPort = 0;
        File.WriteAllText(path, JsonSerializer.Serialize(package));

        InvalidDataException exception = Assert.ThrowsExactly<InvalidDataException>(() => service.ReadPackage(path));

        StringAssert.Contains(exception.Message, "SmtpPort");
        StringAssert.Contains(exception.Message, "actual value: 0");
    }

    [TestMethod]
    public void EncryptedSecretExportRejectsWrongPassphrase()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "settings.json");
        service.ExportToFile(path, true, "correct horse battery staple");
        string json = File.ReadAllText(path);
        Assert.IsFalse(json.Contains("portable-secret", StringComparison.Ordinal));
        ConfigurationTransferPackage package = service.ReadPackage(path);
        Assert.IsNotNull(package.Secrets);
        Assert.AreEqual("Argon2id/AES-256-GCM", package.Secrets.Algorithm);
        Assert.AreEqual(19, package.Secrets.Argon2Version);
        Assert.AreEqual(65536, package.Secrets.MemoryKiB);
        Assert.AreEqual(3, package.Secrets.Iterations);
        Assert.AreEqual(1, package.Secrets.Parallelism);
        Assert.ThrowsExactly<InvalidDataException>(() => service.ImportFromFile(path, Path.Combine(testDirectory, "backups"), "wrong passphrase"));
    }

    [TestMethod]
    public void EncryptedSecretExportRejectsShortPassphrase()
    {
        ConfigurationTransferService service = new(database);
        Assert.ThrowsExactly<ArgumentException>(() => service.Export(true, "too-short"));
    }

    [TestMethod]
    public void EncryptedSecretParametersAreAuthenticatedAndBounded()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "settings.json");
        service.ExportToFile(path, true, "correct horse battery staple");
        ConfigurationTransferPackage package = service.ReadPackage(path);
        package.Secrets!.MemoryKiB--;
        File.WriteAllText(path, JsonSerializer.Serialize(package));
        Assert.ThrowsExactly<InvalidDataException>(() => service.ImportFromFile(path, Path.Combine(testDirectory, "backups"), "correct horse battery staple"));

        package.Secrets.MemoryKiB = 256 * 1024 + 1;
        File.WriteAllText(path, JsonSerializer.Serialize(package));
        Assert.ThrowsExactly<InvalidDataException>(() => service.ReadPackage(path));
    }

    [TestMethod]
    public void Pbkdf2PackageIsRejectedBecauseSchemaOneWasNotReleased()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "settings.json");
        service.ExportToFile(path, true, "correct horse battery staple");
        ConfigurationTransferPackage package = service.ReadPackage(path);
        package.Secrets!.Algorithm = "AES-256-GCM/PBKDF2-SHA256";
        File.WriteAllText(path, JsonSerializer.Serialize(package));
        Assert.ThrowsExactly<InvalidDataException>(() => service.ReadPackage(path));
    }

    [TestMethod]
    public void ImportIsTransactionalAndCreatesVerifiedBackup()
    {
        ConfigurationTransferService service = new(database);
        string path = Path.Combine(testDirectory, "settings.json");
        service.ExportToFile(path);
        ConfigurationTransferPackage package = service.ReadPackage(path);
        package.GlobalPolicy.SoftLockAttempts = 15;
        package.GlobalPolicy.HardLockAttempts = 30;
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(package));

        ConfigurationImportResult result = service.ImportFromFile(path, Path.Combine(testDirectory, "backups"));

        Assert.IsTrue(File.Exists(result.SafetyBackup.FilePath));
        Assert.AreEqual(15L, Convert.ToInt64(database.ExecuteScalar("SELECT SoftLockAttempts FROM Configuration ORDER BY ConfigVersionNumber DESC LIMIT 1")));
        string protectedPassword = Convert.ToString(database.ExecuteScalar("SELECT SmtpPassword FROM Configuration ORDER BY ConfigVersionNumber DESC LIMIT 1")) ?? string.Empty;
        Assert.AreEqual("portable-secret", CryptoHelper.Decrypt(protectedPassword, true));
        Assert.AreEqual("ok", new SqliteMaintenanceService(database).RunIntegrityCheck(true), true);
    }

    private void SaveConfiguration(IddsConfig configuration)
    {
        database.ExecuteNonQuery(@"INSERT INTO Configuration(ConfigVersionDate,HardLockAttempts,HardLockTimeHours,LockForever,SoftLockAttempts,SoftLockTimeMinutes,UseSafeNetworkList,PluginDirectory,LicenseKey,ActivationId,SendInfoMail,SmtpPort,SenderEmailAddress,SmtpRequiresAuthentication,NotificationEmailAddress,SmtpServer,SmtpUsername,SmtpPassword,CyberSheriffContributor,WebBasedMonitoring,HardwareId,SmtpSslRequired) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,NULL,NULL,NULL,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,0,0,NULL,@p15)", DateTime.UtcNow, configuration.HardLockAttempts, configuration.HardLockTimeHours, configuration.LockForever, configuration.SoftLockAttempts, configuration.SoftLockTimeMinutes, configuration.UseSafeNetworkList, configuration.SendInfoMail, configuration.SmtpPort, configuration.SenderEmailAddress, configuration.SmtpRequiresAuthentication, configuration.NotificationEmailAddress, configuration.SmtpServer, configuration.SmtpUsername, configuration.SmtpPassword, configuration.SmtpSslRequired);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Konscious.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供系統設定套件匯出、匯入、加密驗證與預覽服務。
/// </summary>
public sealed class ConfigurationTransferService
{
    private const int MaximumPackageBytes = 4 * 1024 * 1024;
    private const string SecretAlgorithm = "Argon2id/AES-256-GCM";
    private const int Argon2Version = 19;
    private const int DefaultMemoryKiB = 65536;
    private const int DefaultIterations = 3;
    private const int DefaultParallelism = 1;
    private const int MinimumMemoryKiB = 19 * 1024;
    private const int MaximumMemoryKiB = 256 * 1024;
    private const int MaximumIterations = 10;
    private const int MaximumParallelism = 4;
    private const int MinimumPassphraseCharacters = 12;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = false };
    private readonly Database database;

    /// <summary>
    /// 初始化 <see cref="ConfigurationTransferService"/> 類別之新執行個體。
    /// </summary>
    /// <param name="database">資料庫服務執行個體。</param>
    public ConfigurationTransferService(Database database) => this.database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary>
    /// 匯出目前系統設定為資料傳輸套件物件。
    /// </summary>
        /// <param name="includeSecrets">是否包含加密保護之機密資料。</param>
        /// <param name="passphrase">用於衍生加密金鑰之密碼短語。</param>
        /// <returns>匯出之設定傳輸套件執行個體。</returns>
    public ConfigurationTransferPackage Export(bool includeSecrets = false, string? passphrase = null)
    {
        EnsureConfigured();
        ConfigurationRow configuration = database.Query<ConfigurationRow>("SELECT * FROM Configuration ORDER BY ConfigVersionNumber DESC LIMIT 1").Single();
        Dictionary<string, string> applicationSettings = database.Query<KeyValueRow>("SELECT ConfigKey, ConfigValue FROM AppConfig")
            .ToDictionary(item => item.ConfigKey, item => item.ConfigValue ?? string.Empty, StringComparer.Ordinal);
        if (!applicationSettings.TryGetValue(IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE, out string? firewallMode)
            || string.IsNullOrWhiteSpace(firewallMode))
        {
            applicationSettings[IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE] = FirewallBlockMode.Inbound.ToString();
        }
        ConfigurationTransferPackage package = new()
        {
            GlobalPolicy = new GlobalConfigurationTransfer
            {
                HardLockAttempts = configuration.HardLockAttempts,
                HardLockTimeHours = configuration.HardLockTimeHours,
                LockForever = configuration.LockForever,
                SoftLockAttempts = configuration.SoftLockAttempts,
                SoftLockTimeMinutes = configuration.SoftLockTimeMinutes,
                UseSafeNetworkList = configuration.UseSafeNetworkList,
                SendInfoMail = configuration.SendInfoMail,
                SmtpPort = configuration.SmtpPort,
                SenderEmailAddress = configuration.SenderEmailAddress ?? string.Empty,
                SmtpRequiresAuthentication = configuration.SmtpRequiresAuthentication,
                NotificationEmailAddress = configuration.NotificationEmailAddress ?? string.Empty,
                SmtpServer = configuration.SmtpServer ?? string.Empty,
                SmtpUsername = configuration.SmtpUsername ?? string.Empty,
                SmtpSslRequired = configuration.SmtpSslRequired
            },
            ApplicationSettings = applicationSettings,
            SafeNetworks = database.Query<NetworkRow>("SELECT IpAddress, NetworkMask FROM WhiteList").Select(item => new SafeNetworkTransfer(item.IpAddress, item.NetworkMask)).ToList(),
            Agents = ReadAgents()
        };
        if (includeSecrets)
        {
            if (string.IsNullOrWhiteSpace(passphrase)) throw Argument("A passphrase is required when exporting secrets.", nameof(passphrase));
            if (passphrase.Length < MinimumPassphraseCharacters) throw Argument("The configuration package passphrase must contain at least 12 characters.", nameof(passphrase));
            string clearPassword = string.IsNullOrEmpty(configuration.SmtpPassword) ? string.Empty : CryptoHelper.Decrypt(configuration.SmtpPassword, true);
            package.Secrets = EncryptSecret(clearPassword, passphrase);
        }
        Validate(package);
        return package;
    }

    /// <summary>
    /// 將目前系統設定匯出並寫入指定的 JSON 檔案路徑。
    /// </summary>
        /// <param name="path">目標檔案路徑。</param>
        /// <param name="includeSecrets">是否包含加密保護之機密資料。</param>
        /// <param name="passphrase">用於衍生加密金鑰之密碼短語。</param>
    public void ExportToFile(string path, bool includeSecrets = false, string? passphrase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory);
        string temporary = fullPath + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(Export(includeSecrets, passphrase), JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, fullPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>
    /// 從指定檔案讀取並驗證結構完整性之設定傳輸套件。
    /// </summary>
        /// <param name="path">來源檔案路徑。</param>
        /// <returns>解析後的設定傳輸套件。</returns>
        /// <exception cref="InvalidDataException">當檔案格式或內容不合法時拋出。</exception>
    public ConfigurationTransferPackage ReadPackage(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileInfo file = new(path);
        if (!file.Exists) throw MissingFile(path);
        if (file.Length is <= 0 or > MaximumPackageBytes) throw Invalid("Configuration package size is invalid.");
        ConfigurationTransferPackage package = JsonSerializer.Deserialize<ConfigurationTransferPackage>(File.ReadAllText(file.FullName, Encoding.UTF8), JsonOptions) ?? throw Invalid("Configuration package is empty.");
        Validate(package);
        return package;
    }

    /// <summary>
    /// 預覽設定傳輸套件內容以供使用者確認。
    /// </summary>
        /// <param name="package">欲預覽之設定傳輸套件。</param>
        /// <returns>匯入預覽資訊。</returns>
    public ConfigurationImportPreview Preview(ConfigurationTransferPackage package)
    {
        EnsureConfigured();
        Validate(package);
        HashSet<Guid> installed = database.Query<string>("SELECT AgentId FROM SecurityAgents").Select(Guid.Parse).ToHashSet();
        Guid[] unknown = package.Agents.Select(agent => agent.AgentId).Where(id => !installed.Contains(id)).ToArray();
        return new ConfigurationImportPreview(package.Agents.Count, package.SafeNetworks.Count, package.ApplicationSettings.Count, package.Secrets is not null, unknown);
    }

    /// <summary>
    /// 從指定的設定檔案匯入組態，自動建立安全備份並於交易中套用。
    /// </summary>
        /// <param name="path">來源檔案路徑。</param>
        /// <param name="backupDirectory">安全備份目錄。</param>
        /// <param name="passphrase">用於解密機密資料之密碼短語。</param>
        /// <returns>匯入執行結果。</returns>
    public ConfigurationImportResult ImportFromFile(string path, string backupDirectory, string? passphrase = null)
    {
        ConfigurationTransferPackage package = ReadPackage(path);
        ConfigurationImportPreview preview = Preview(package);
        if (package.Secrets is not null && (string.IsNullOrWhiteSpace(passphrase) || passphrase.Length < MinimumPassphraseCharacters))
            throw Argument("The configuration package passphrase must contain at least 12 characters.", nameof(passphrase));
        string? smtpPassword = package.Secrets is null ? null : DecryptSecret(package.Secrets, passphrase!);
        DatabaseBackupResult backup = new SqliteMaintenanceService(database).CreateVerifiedBackup(backupDirectory);
        database.ExecuteInTransaction((connection, transaction) => Apply(connection, transaction, package, smtpPassword));
        return new ConfigurationImportResult(backup, preview);
    }

    private List<AgentConfigurationTransfer> ReadAgents()
    {
        List<AgentConfigurationTransfer> agents = database.Query<AgentRow>("SELECT * FROM SecurityAgents ORDER BY Name").Select(row => new AgentConfigurationTransfer
        {
            AgentId = Guid.Parse(row.AgentId),
            Name = row.Name,
            AssemblyName = Path.GetFileName(row.AssemblyName ?? string.Empty),
            HardLockAttempts = row.HardLockAttempts,
            HardLockTimeHours = row.HardLockTimeHours,
            LockForever = row.LockForever,
            SoftLockAttempts = row.SoftLockAttempts,
            SoftLockTimeMinutes = row.SoftLockTimeMinutes,
            OverrideConfiguration = row.OverwriteConfiguration,
            DisplayName = row.DisplayName,
            Enabled = row.Enabled
        }).ToList();
        foreach (AgentConfigurationTransfer agent in agents)
            agent.Settings = database.Query<KeyValueRow>("SELECT PropertyName AS ConfigKey, PropertyValueString AS ConfigValue FROM SecurityAgentConfig WHERE AgentId=@AgentId", new { AgentId = agent.AgentId }).ToDictionary(item => item.ConfigKey, item => item.ConfigValue ?? string.Empty, StringComparer.Ordinal);
        return agents;
    }

    private static void Apply(SqliteConnection connection, SqliteTransaction transaction, ConfigurationTransferPackage package, string? smtpPassword)
    {
        GlobalConfigurationTransfer policy = package.GlobalPolicy;
        string protectedPassword = smtpPassword is null
            ? connection.ExecuteScalar<string?>("SELECT SmtpPassword FROM Configuration ORDER BY ConfigVersionNumber DESC LIMIT 1", transaction: transaction) ?? string.Empty
            : CryptoHelper.Encrypt(smtpPassword, true);
        connection.Execute(@"INSERT INTO Configuration(ConfigVersionDate,HardLockAttempts,HardLockTimeHours,LockForever,SoftLockAttempts,SoftLockTimeMinutes,UseSafeNetworkList,PluginDirectory,LicenseKey,ActivationId,SendInfoMail,SmtpPort,SenderEmailAddress,SmtpRequiresAuthentication,NotificationEmailAddress,SmtpServer,SmtpUsername,SmtpPassword,CyberSheriffContributor,WebBasedMonitoring,HardwareId,SmtpSslRequired)
VALUES(@Now,@HardLockAttempts,@HardLockTimeHours,@LockForever,@SoftLockAttempts,@SoftLockTimeMinutes,@UseSafeNetworkList,NULL,NULL,NULL,@SendInfoMail,@SmtpPort,@SenderEmailAddress,@SmtpRequiresAuthentication,@NotificationEmailAddress,@SmtpServer,@SmtpUsername,@SmtpPassword,0,0,NULL,@SmtpSslRequired)", new { Now = DateTime.UtcNow, policy.HardLockAttempts, policy.HardLockTimeHours, policy.LockForever, policy.SoftLockAttempts, policy.SoftLockTimeMinutes, policy.UseSafeNetworkList, policy.SendInfoMail, policy.SmtpPort, policy.SenderEmailAddress, policy.SmtpRequiresAuthentication, policy.NotificationEmailAddress, policy.SmtpServer, policy.SmtpUsername, SmtpPassword = protectedPassword, policy.SmtpSslRequired }, transaction);
        connection.Execute("DELETE FROM AppConfig", transaction: transaction);
        foreach ((string key, string value) in package.ApplicationSettings) connection.Execute("INSERT INTO AppConfig(ConfigKey,ConfigValue) VALUES(@Key,@Value)", new { Key = key, Value = value }, transaction);
        connection.Execute("DELETE FROM WhiteList", transaction: transaction);
        foreach (SafeNetworkTransfer network in package.SafeNetworks) connection.Execute("INSERT INTO WhiteList(IpAddress,NetworkMask) VALUES(@IpAddress,@NetworkMask)", network, transaction);
        foreach (AgentConfigurationTransfer agent in package.Agents)
        {
            int exists = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SecurityAgents WHERE AgentId=@AgentId", new { agent.AgentId }, transaction);
            if (exists == 0) continue;
            connection.Execute(@"UPDATE SecurityAgents SET HardLockAttempts=@HardLockAttempts,HardLockTimeHours=@HardLockTimeHours,LockForever=@LockForever,SoftLockAttempts=@SoftLockAttempts,SoftLockTimeMinutes=@SoftLockTimeMinutes,OverwriteConfiguration=@OverrideConfiguration,Enabled=@Enabled,Serial=Serial+1 WHERE AgentId=@AgentId", agent, transaction);
            connection.Execute("DELETE FROM SecurityAgentConfig WHERE AgentId=@AgentId", new { agent.AgentId }, transaction);
            foreach ((string key, string value) in agent.Settings) connection.Execute("INSERT INTO SecurityAgentConfig(AgentId,PropertyName,PropertyValueString) VALUES(@AgentId,@Key,@Value)", new { agent.AgentId, Key = key, Value = value }, transaction);
        }
        connection.Execute("INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@OccurredUtc,'Configuration.Import','Succeeded',@Actor,'Configuration','')", new { OccurredUtc = DateTimeOffset.UtcNow.ToString("O"), Actor = Environment.UserDomainName + "\\" + Environment.UserName }, transaction);
    }

    private static void Validate(ConfigurationTransferPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (package.Format != ConfigurationTransferPackage.CurrentFormat || package.SchemaVersion != ConfigurationTransferPackage.CurrentSchemaVersion) throw Invalid("Unsupported configuration package format or schema version.");
        if (package.GlobalPolicy is null || package.ApplicationSettings is null || package.SafeNetworks is null || package.Agents is null) throw Invalid("Required configuration sections are missing.");
        if (package.GlobalPolicy.SoftLockAttempts is < 1 or > 100000)
            throw Invalid($"SoftLockAttempts must be between 1 and 100000; actual value: {package.GlobalPolicy.SoftLockAttempts}.");
        if (package.GlobalPolicy.HardLockAttempts < package.GlobalPolicy.SoftLockAttempts)
            throw Invalid($"HardLockAttempts ({package.GlobalPolicy.HardLockAttempts}) must be greater than or equal to SoftLockAttempts ({package.GlobalPolicy.SoftLockAttempts}).");
        if (package.GlobalPolicy.SmtpPort is < 1 or > 65535)
            throw Invalid($"SmtpPort must be between 1 and 65535; actual value: {package.GlobalPolicy.SmtpPort}.");
        if (package.GlobalPolicy.SmtpServer is null || package.GlobalPolicy.SmtpServer.Length > 250)
            throw Invalid($"SmtpServer length is invalid; length: {package.GlobalPolicy.SmtpServer?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
        if (package.GlobalPolicy.SmtpUsername is null || package.GlobalPolicy.SmtpUsername.Length > 250)
            throw Invalid($"SmtpUsername length is invalid; length: {package.GlobalPolicy.SmtpUsername?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
        if (package.GlobalPolicy.SenderEmailAddress is null || package.GlobalPolicy.SenderEmailAddress.Length > 250)
            throw Invalid($"SenderEmailAddress length is invalid; length: {package.GlobalPolicy.SenderEmailAddress?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
        if (package.GlobalPolicy.NotificationEmailAddress is null || package.GlobalPolicy.NotificationEmailAddress.Length > 250)
            throw Invalid($"NotificationEmailAddress length is invalid; length: {package.GlobalPolicy.NotificationEmailAddress?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
        if (package.ApplicationSettings.Count > 10000 || package.SafeNetworks.Count > 10000 || package.Agents.Count > 1000) throw Invalid("Configuration package exceeds supported limits.");
        foreach ((string key, string value) in package.ApplicationSettings)
        {
            if (key.Length is 0 or > 250)
                throw Invalid($"Application setting name length is invalid; key: '{key}', length: {key.Length}.");
            if (value is null || value.Length > 250)
                throw Invalid($"Application setting value length is invalid; key: '{key}', length: {value?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
        }
        if (package.ApplicationSettings.TryGetValue(IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE, out string? firewallMode)
            && (!Enum.TryParse(firewallMode, true, out FirewallBlockMode parsedMode) || !Enum.IsDefined(parsedMode)))
            throw Invalid($"The firewall blocking mode '{firewallMode}' is unsupported.");
        foreach (SafeNetworkTransfer network in package.SafeNetworks)
        {
            if (!IPAddress.TryParse(network.IpAddress, out IPAddress? address)) throw Invalid($"Invalid safe-network address: {network.IpAddress}");
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) { if (!int.TryParse(network.NetworkMask, out int prefix) || prefix is < 0 or > 128) throw Invalid("Invalid IPv6 prefix length."); }
            else
            {
                try
                {
                    _ = IddsConfig.GetSubnetMaskBits(network.NetworkMask);
                }
                catch (ArgumentException exception)
                {
                    throw Invalid($"Invalid IPv4 network mask: {network.NetworkMask}", exception);
                }
            }
        }
        Guid[] duplicateAgentIds = package.Agents.GroupBy(agent => agent.AgentId).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateAgentIds.Length > 0)
            throw Invalid($"Agent identifiers must be unique; duplicates: {string.Join(", ", duplicateAgentIds)}.");
        foreach (AgentConfigurationTransfer agent in package.Agents)
        {
            if (agent.AgentId == Guid.Empty)
                throw Invalid($"Agent '{agent.Name}' has an empty identifier.");
            if (agent.Settings is null)
                throw Invalid($"Agent '{agent.Name}' ({agent.AgentId}) has no settings collection.");
            if (agent.Settings.Count > 1000)
                throw Invalid($"Agent '{agent.Name}' ({agent.AgentId}) exceeds the 1000-setting limit; actual count: {agent.Settings.Count}.");
            foreach ((string key, string value) in agent.Settings)
            {
                if (key.Length is 0 or > 255)
                    throw Invalid($"Agent '{agent.Name}' ({agent.AgentId}) has an invalid setting name length; key: '{key}', length: {key.Length}.");
                if (value is null || value.Length > 4000)
                    throw Invalid($"Agent '{agent.Name}' ({agent.AgentId}) setting '{key}' has an invalid value length: {value?.Length.ToString(CultureInfo.InvariantCulture) ?? "null"}.");
            }
        }
        if (package.Secrets is not null) ValidateSecretEncoding(package.Secrets);
    }

    private static EncryptedSecretTransfer EncryptSecret(string secret, string passphrase)
    {
        EncryptedSecretTransfer result = new()
        {
            Algorithm = SecretAlgorithm,
            Argon2Version = Argon2Version,
            MemoryKiB = DefaultMemoryKiB,
            Iterations = DefaultIterations,
            Parallelism = DefaultParallelism
        };
        byte[] salt = RandomNumberGenerator.GetBytes(16); byte[] nonce = RandomNumberGenerator.GetBytes(12); byte[] tag = new byte[16]; byte[] plaintext = Encoding.UTF8.GetBytes(secret); byte[] ciphertext = new byte[plaintext.Length];
        byte[] key = DeriveKey(passphrase, salt, result);
        byte[] associatedData = CreateAssociatedData(result);
        try { using AesGcm aes = new(key, 16); aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData); }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plaintext); }
        result.Salt = Convert.ToBase64String(salt); result.Nonce = Convert.ToBase64String(nonce); result.Ciphertext = Convert.ToBase64String(ciphertext); result.Tag = Convert.ToBase64String(tag);
        return result;
    }

    private static string DecryptSecret(EncryptedSecretTransfer secret, string passphrase)
    {
        ValidateSecretParameters(secret);
        byte[] salt = Convert.FromBase64String(secret.Salt); byte[] nonce = Convert.FromBase64String(secret.Nonce); byte[] ciphertext = Convert.FromBase64String(secret.Ciphertext); byte[] tag = Convert.FromBase64String(secret.Tag);
        if (salt.Length != 16 || nonce.Length != 12 || tag.Length != 16 || ciphertext.Length > MaximumPackageBytes) throw Invalid("Invalid secret encryption parameters.");
        byte[] plaintext = new byte[ciphertext.Length];
        byte[] key = DeriveKey(passphrase, salt, secret);
        byte[] associatedData = CreateAssociatedData(secret);
        try { using AesGcm aes = new(key, 16); aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData); return Encoding.UTF8.GetString(plaintext); }
        catch (CryptographicException exception) { throw Invalid("The configuration secret passphrase is invalid or the package was modified.", exception); }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plaintext); }
    }

    internal static byte[] DeriveKey(string passphrase, byte[] salt, EncryptedSecretTransfer parameters)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(passphrase);
        try
        {
            using Argon2id argon2 = new(passwordBytes)
            {
                Salt = salt,
                MemorySize = parameters.MemoryKiB,
                Iterations = parameters.Iterations,
                DegreeOfParallelism = parameters.Parallelism
            };
            return argon2.GetBytes(32);
        }
        finally { CryptographicOperations.ZeroMemory(passwordBytes); }
    }

    private static byte[] CreateAssociatedData(EncryptedSecretTransfer parameters) => Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{ConfigurationTransferPackage.CurrentFormat}\nschema={ConfigurationTransferPackage.CurrentSchemaVersion}\nalgorithm={parameters.Algorithm}\nargon2Version={parameters.Argon2Version}\nmemoryKiB={parameters.MemoryKiB}\niterations={parameters.Iterations}\nparallelism={parameters.Parallelism}"));

    private static void ValidateSecretParameters(EncryptedSecretTransfer secret)
    {
        if (secret.Algorithm != SecretAlgorithm || secret.Argon2Version != Argon2Version) throw Invalid("Unsupported secret encryption parameters.");
        if (secret.MemoryKiB is < MinimumMemoryKiB or > MaximumMemoryKiB || secret.Iterations is < 2 or > MaximumIterations || secret.Parallelism is < 1 or > MaximumParallelism) throw Invalid("Invalid secret encryption parameters.");
    }

    private static void ValidateSecretEncoding(EncryptedSecretTransfer secret)
    {
        ValidateSecretParameters(secret);
        try
        {
            byte[] salt = Convert.FromBase64String(secret.Salt);
            byte[] nonce = Convert.FromBase64String(secret.Nonce);
            byte[] ciphertext = Convert.FromBase64String(secret.Ciphertext);
            byte[] tag = Convert.FromBase64String(secret.Tag);
            if (salt.Length != 16 || nonce.Length != 12 || tag.Length != 16 || ciphertext.Length > MaximumPackageBytes) throw Invalid("Invalid secret encryption parameters.");
        }
        catch (FormatException exception) { throw Invalid("Invalid secret encryption parameters.", exception); }
    }

    private void EnsureConfigured() { if (!database.IsConfigured) throw InvalidOperation("Database is not configured."); }
    private static ArgumentException Argument(string message, string name) => new(message, name);
    private static FileNotFoundException MissingFile(string path) => new("Configuration package was not found.", path);
    private static InvalidDataException Invalid(string message, Exception? inner = null) => new(message, inner);
    private static InvalidOperationException InvalidOperation(string message) => new(message);
    private sealed class ConfigurationRow { /// <summary>
/// 取得或設定 硬封鎖失敗次數門檻。
/// </summary>
public int HardLockAttempts { get; init; } /// <summary>
/// 取得或設定 硬封鎖持續時數。
/// </summary>
public int HardLockTimeHours { get; init; } /// <summary>
/// 取得或設定 是否永久封鎖。
/// </summary>
public bool LockForever { get; init; } /// <summary>
/// 取得或設定 軟封鎖失敗次數門檻。
/// </summary>
public int SoftLockAttempts { get; init; } /// <summary>
/// 取得或設定 軟封鎖持續分鐘數。
/// </summary>
public int SoftLockTimeMinutes { get; init; } /// <summary>
/// 取得或設定 是否使用安全網路清單。
/// </summary>
public bool UseSafeNetworkList { get; init; } /// <summary>
/// 取得或設定 SendInfoMail。
/// </summary>
public bool SendInfoMail { get; init; } /// <summary>
/// 取得或設定 SMTP 連接埠。
/// </summary>
public int SmtpPort { get; init; } /// <summary>
/// 取得或設定 寄件者電子郵件地址。
/// </summary>
public string? SenderEmailAddress { get; init; } /// <summary>
/// 取得或設定 SmtpRequiresAuthentication。
/// </summary>
public bool SmtpRequiresAuthentication { get; init; } /// <summary>
/// 取得或設定 通知收件者電子郵件地址。
/// </summary>
public string? NotificationEmailAddress { get; init; } /// <summary>
/// 取得或設定 SMTP 伺服器位址。
/// </summary>
public string? SmtpServer { get; init; } /// <summary>
/// 取得或設定 SMTP 帳號名稱。
/// </summary>
public string? SmtpUsername { get; init; } /// <summary>
/// 取得或設定 SMTP 密碼。
/// </summary>
public string? SmtpPassword { get; init; } /// <summary>
/// 取得或設定 SMTP 是否要求 SSL。
/// </summary>
public bool SmtpSslRequired { get; init; } }
    private sealed class KeyValueRow { /// <summary>
/// 取得或設定 ConfigKey。
/// </summary>
public string ConfigKey { get; init; } = string.Empty; /// <summary>
/// 取得或設定 ConfigValue。
/// </summary>
public string? ConfigValue { get; init; } }
    private sealed class NetworkRow { /// <summary>
/// 取得或設定 IpAddress。
/// </summary>
public string IpAddress { get; init; } = string.Empty; /// <summary>
/// 取得或設定 NetworkMask。
/// </summary>
public string NetworkMask { get; init; } = string.Empty; }
    private sealed class AgentRow { /// <summary>
/// 取得或設定 AgentId。
/// </summary>
public string AgentId { get; init; } = string.Empty; /// <summary>
/// 取得或設定 Name。
/// </summary>
public string Name { get; init; } = string.Empty; /// <summary>
/// 取得或設定 AssemblyName。
/// </summary>
public string? AssemblyName { get; init; } /// <summary>
/// 取得或設定 硬封鎖失敗次數門檻。
/// </summary>
public int HardLockAttempts { get; init; } /// <summary>
/// 取得或設定 硬封鎖持續時數。
/// </summary>
public int HardLockTimeHours { get; init; } /// <summary>
/// 取得或設定 是否永久封鎖。
/// </summary>
public bool LockForever { get; init; } /// <summary>
/// 取得或設定 軟封鎖失敗次數門檻。
/// </summary>
public int SoftLockAttempts { get; init; } /// <summary>
/// 取得或設定 軟封鎖持續分鐘數。
/// </summary>
public int SoftLockTimeMinutes { get; init; } /// <summary>
/// 取得或設定 OverwriteConfiguration。
/// </summary>
public bool OverwriteConfiguration { get; init; } /// <summary>
/// 取得或設定 本地化顯示名稱。
/// </summary>
public string DisplayName { get; init; } = string.Empty; /// <summary>
/// 取得或設定 是否已啟用。
/// </summary>
public bool Enabled { get; init; } }
}

using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表可攜式系統組態匯出與匯入資料封裝套件。
/// </summary>
public sealed class ConfigurationTransferPackage
{
    /// <summary>
    /// 定義目前支援的組態套件格式標頭識別碼。
    /// </summary>
    public const string CurrentFormat = "IDDSCommunity.Configuration";

    /// <summary>
    /// 定義目前支援的組態結構版本號碼。
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// 取得或設定組態套件格式識別碼。
    /// </summary>
    public string Format { get; set; } = CurrentFormat;

    /// <summary>
    /// 取得或設定組態結構版本號碼。
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// 取得或設定匯出時的產品版本字串。
    /// </summary>
    public string ProductVersion { get; set; } = "3.0.0";

    /// <summary>
    /// 取得或設定匯出作業發生的 UTC 時間戳記。
    /// </summary>
    public DateTimeOffset ExportedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 取得或設定全域防護策略與 SMTP 設定。
    /// </summary>
    public GlobalConfigurationTransfer GlobalPolicy { get; set; } = new();

    /// <summary>
    /// 取得或設定應用程式設定機碼與值字典。
    /// </summary>
    public Dictionary<string, string> ApplicationSettings { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 取得或設定安全網路規則清單。
    /// </summary>
    public List<SafeNetworkTransfer> SafeNetworks { get; set; } = [];

    /// <summary>
    /// 取得或設定個別代理程式組態清單。
    /// </summary>
    public List<AgentConfigurationTransfer> Agents { get; set; } = [];

    /// <summary>
    /// 取得或設定加密封裝之機密資料，若匯出時未勾選則為 null。
    /// </summary>
    public EncryptedSecretTransfer? Secrets { get; set; }
}

/// <summary>
/// 代表全域防護策略與 SMTP 設定之傳輸資料模型。
/// </summary>
public sealed class GlobalConfigurationTransfer
{
    /// <summary>
    /// 取得或設定觸發硬封鎖之失敗次數門檻。
    /// </summary>
    public int HardLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定硬封鎖持續時數。
    /// </summary>
    public int HardLockTimeHours { get; set; }

    /// <summary>
    /// 取得或設定是否永久封鎖。
    /// </summary>
    public bool LockForever { get; set; }

    /// <summary>
    /// 取得或設定觸發軟封鎖之失敗次數門檻。
    /// </summary>
    public int SoftLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定軟封鎖持續分鐘數。
    /// </summary>
    public int SoftLockTimeMinutes { get; set; }

    /// <summary>
    /// 取得或設定是否啟用安全網路允許清單過濾。
    /// </summary>
    public bool UseSafeNetworkList { get; set; }

    /// <summary>
    /// 取得或設定是否寄送通知郵件。
    /// </summary>
    public bool SendInfoMail { get; set; }

    /// <summary>
    /// 取得或設定 SMTP 伺服器連接埠號。
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// 取得或設定寄件者電子郵件地址。
    /// </summary>
    public string SenderEmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 SMTP 伺服器是否需要身分驗證。
    /// </summary>
    public bool SmtpRequiresAuthentication { get; set; }

    /// <summary>
    /// 取得或設定警報通知收件者電子郵件地址。
    /// </summary>
    public string NotificationEmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 SMTP 郵件伺服器主機名稱或 IP 位址。
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 SMTP 驗證使用者名稱。
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 SMTP 連線是否要求 SSL/TLS 加密。
    /// </summary>
    public bool SmtpSslRequired { get; set; }
}

/// <summary>
/// 代表安全網路 IP 與遮罩項目之傳輸資料模型。
/// </summary>
/// <param name="IpAddress">IP 位址字串。</param>
/// <param name="NetworkMask">網路遮罩字串。</param>
public sealed record SafeNetworkTransfer(string IpAddress, string NetworkMask);

/// <summary>
/// 代表個別安全性代理程式之匯出組態資料模型。
/// </summary>
public sealed class AgentConfigurationTransfer
{
    /// <summary>
    /// 取得或設定代理程式唯一識別碼 (GUID)。
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// 取得或設定代理程式系統名稱。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定代理程式組件檔案名稱。
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定硬封鎖失敗次數門檻。
    /// </summary>
    public int HardLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定硬封鎖持續時數。
    /// </summary>
    public int HardLockTimeHours { get; set; }

    /// <summary>
    /// 取得或設定是否永久封鎖。
    /// </summary>
    public bool LockForever { get; set; }

    /// <summary>
    /// 取得或設定軟封鎖失敗次數門檻。
    /// </summary>
    public int SoftLockAttempts { get; set; }

    /// <summary>
    /// 取得或設定軟封鎖持續分鐘數。
    /// </summary>
    public int SoftLockTimeMinutes { get; set; }

    /// <summary>
    /// 取得或設定是否覆寫全域策略組態。
    /// </summary>
    public bool OverrideConfiguration { get; set; }

    /// <summary>
    /// 取得或設定代理程式本地化顯示名稱。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定代理程式是否啟用。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 取得或設定代理程式自訂屬性設定字典。
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// 代表經過 Argon2id 衍生金鑰與 AES-256-GCM 加密之機密資料傳輸封裝。
/// </summary>
public sealed class EncryptedSecretTransfer
{
    /// <summary>
    /// 取得或設定加密與金鑰衍生演算法識別字串。
    /// </summary>
    public string Algorithm { get; set; } = "Argon2id/AES-256-GCM";

    /// <summary>
    /// 取得或設定 Argon2 演算法規格版本號碼。
    /// </summary>
    public int Argon2Version { get; set; } = 19;

    /// <summary>
    /// 取得或設定 Argon2 記憶體消耗大小 (KiB)。
    /// </summary>
    public int MemoryKiB { get; set; } = 65536;

    /// <summary>
    /// 取得或設定 Argon2 迭代次數。
    /// </summary>
    public int Iterations { get; set; } = 3;

    /// <summary>
    /// 取得或設定 Argon2 平行運算執行緒數。
    /// </summary>
    public int Parallelism { get; set; } = 1;

    /// <summary>
    /// 取得或設定 Base64 編碼之金鑰衍生鹽值 (Salt)。
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Base64 編碼之 AES-GCM 隨機 Nonce。
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Base64 編碼之密文字串。
    /// </summary>
    public string Ciphertext { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Base64 編碼之 AES-GCM 驗證標籤 (Authentication Tag)。
    /// </summary>
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// 代表組態匯入前之結構預覽與未識別代理程式統計資訊。
/// </summary>
/// <param name="AgentCount">套件內含之代理程式數量。</param>
/// <param name="SafeNetworkCount">套件內含之安全網路規則數量。</param>
/// <param name="ApplicationSettingCount">套件內含之應用程式設定數量。</param>
/// <param name="ContainsSecrets">套件是否包含加密機密資料。</param>
/// <param name="UnknownAgentIds">目前本機尚未安裝之未知代理程式 ID 清單。</param>
public sealed record ConfigurationImportPreview(int AgentCount, int SafeNetworkCount, int ApplicationSettingCount, bool ContainsSecrets, IReadOnlyList<Guid> UnknownAgentIds);

/// <summary>
/// 代表組態匯入成功後之安全備份與預覽結果。
/// </summary>
/// <param name="SafetyBackup">匯入前建立之資料庫安全備份結果。</param>
/// <param name="Preview">套用的組態預覽統計資料。</param>
public sealed record ConfigurationImportResult(DatabaseBackupResult SafetyBackup, ConfigurationImportPreview Preview);

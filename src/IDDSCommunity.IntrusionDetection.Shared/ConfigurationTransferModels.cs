using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared;

public sealed class ConfigurationTransferPackage
{
    public const string CurrentFormat = "IDDSCommunity.Configuration";
    public const int CurrentSchemaVersion = 1;
    public string Format { get; set; } = CurrentFormat;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ProductVersion { get; set; } = "3.0.0";
    public DateTimeOffset ExportedUtc { get; set; } = DateTimeOffset.UtcNow;
    public GlobalConfigurationTransfer GlobalPolicy { get; set; } = new();
    public Dictionary<string, string> ApplicationSettings { get; set; } = new(StringComparer.Ordinal);
    public List<SafeNetworkTransfer> SafeNetworks { get; set; } = [];
    public List<AgentConfigurationTransfer> Agents { get; set; } = [];
    public EncryptedSecretTransfer? Secrets { get; set; }
}

public sealed class GlobalConfigurationTransfer
{
    public int HardLockAttempts { get; set; }
    public int HardLockTimeHours { get; set; }
    public bool LockForever { get; set; }
    public int SoftLockAttempts { get; set; }
    public int SoftLockTimeMinutes { get; set; }
    public bool UseSafeNetworkList { get; set; }
    public bool SendInfoMail { get; set; }
    public int SmtpPort { get; set; }
    public string SenderEmailAddress { get; set; } = string.Empty;
    public bool SmtpRequiresAuthentication { get; set; }
    public string NotificationEmailAddress { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public string SmtpUsername { get; set; } = string.Empty;
    public bool SmtpSslRequired { get; set; }
}

public sealed record SafeNetworkTransfer(string IpAddress, string NetworkMask);

public sealed class AgentConfigurationTransfer
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public int HardLockAttempts { get; set; }
    public int HardLockTimeHours { get; set; }
    public bool LockForever { get; set; }
    public int SoftLockAttempts { get; set; }
    public int SoftLockTimeMinutes { get; set; }
    public bool OverrideConfiguration { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.Ordinal);
}

public sealed class EncryptedSecretTransfer
{
    public string Algorithm { get; set; } = "Argon2id/AES-256-GCM";
    public int Argon2Version { get; set; } = 19;
    public int MemoryKiB { get; set; } = 65536;
    public int Iterations { get; set; } = 3;
    public int Parallelism { get; set; } = 1;
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Ciphertext { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

public sealed record ConfigurationImportPreview(int AgentCount, int SafeNetworkCount, int ApplicationSettingCount, bool ContainsSecrets, IReadOnlyList<Guid> UnknownAgentIds);
public sealed record ConfigurationImportResult(DatabaseBackupResult SafetyBackup, ConfigurationImportPreview Preview);

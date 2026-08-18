using System;


namespace IDDSCommunity.IntrusionDetection.Shared.Db;



/// <summary>
/// 執行資料庫結構版本 2.1 升級邏輯之腳本類別。
/// </summary>
public class Version_2_1 : DbUpgradeScript
{
        /// <summary>
    /// 取得或設定 INTERNAL_VERSION。
    /// </summary>
public override int INTERNAL_VERSION => 1;

        /// <summary>
    /// 定義 TABLE_DB_CONFIG 之數值。
    /// </summary>
public const string TABLE_DB_CONFIG = @"CREATE TABLE DbConfig(Version bigint NOT NULL, UpgradeDate DateTime NOT NULL, UpgradeLog nvarchar(1000), UpgradeSuccessful bit NOT NULL)";

        /// <summary>
    /// 定義 TABLE_CONFIGURATION 之數值。
    /// </summary>
public const string TABLE_CONFIGURATION = @"
CREATE TABLE Configuration (
    ConfigVersionNumber INTEGER PRIMARY KEY AUTOINCREMENT not null,
    ConfigVersionDate DateTime NULL,
	HardLockAttempts int NOT NULL,
	HardLockTimeHours int NOT NULL,
	LockForever bit NOT NULL,
	SoftLockAttempts int NOT NULL,
	SoftLockTimeMinutes int NOT NULL,
	UseSafeNetworkList bit NOT NULL,
	PluginDirectory nvarchar(255) NULL,
	LicenseKey nvarchar(255) NULL,
	ActivationId nvarchar(255) NULL,
    HardwareId nvarchar(255) NULL,
	SendInfoMail bit NOT NULL,
	SmtpPort int NOT NULL,
	SenderEmailAddress nvarchar(255) NULL,
	SmtpRequiresAuthentication bit NOT NULL,
	NotificationEmailAddress nvarchar(255) NULL,
	SmtpServer nvarchar(255) NULL,
	SmtpUsername nvarchar(255) NULL,
	SmtpPassword nvarchar(255) NULL,
    SmtpSslRequired bit NOT NULL,
	CyberSheriffContributor bit NOT NULL,
	WebBasedMonitoring bit NOT NULL
)";

        /// <summary>
    /// 定義 CREATE_DEFAULT_CONFIGURATION 之數值。
    /// </summary>
public const string CREATE_DEFAULT_CONFIGURATION = @"
INSERT INTO Configuration(ConfigVersionDate, HardLockAttempts, HardLockTimeHours, LockForever,
                SoftLockAttempts, SoftLockTimeMinutes, UseSafeNetworkList, SendInfoMail, SmtpPort,
                SmtpRequiresAuthentication, SmtpSslRequired, CyberSheriffContributor, WebBasedMonitoring)
        values('4/4/2013',20,1,0,10,1,0,0,25,0,0,0,0)";

        /// <summary>
    /// 定義 CREATE_DEFAULT_DB_CONFIGURATION 之數值。
    /// </summary>
public const string CREATE_DEFAULT_DB_CONFIGURATION = @"
INSERT INTO DbConfig(Version, UpgradeDate, UpgradeLog, UpgradeSuccessful)
        values(1,'now','Initial setup',1)
";

        /// <summary>
    /// 定義 TABLE_INTRUSION_LOG 之數值。
    /// </summary>
public const string TABLE_INTRUSION_LOG = @"
CREATE TABLE IntrusionLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT not null,
    IncidentTime DateTime null,
    AgentId uniqueidentifier null,
    ClientIP nvarchar(80) null,
    Action int null,
    ActionTriggeredByUser bit null
)";
        /// <summary>
    /// 定義 TABLE_LOCKS 之數值。
    /// </summary>
public const string TABLE_LOCKS = @"
CREATE TABLE Locks (
    LockId INTEGER PRIMARY KEY AUTOINCREMENT not null,
    LockDate DateTime not null,
    IpAddress nvarchar(60),
    Port int null,
    UnlockDate DateTime null,
    TriggerIncident bigint not null,
    Status int not null,
    LastUpdate DateTime null
)";
        /// <summary>
    /// 定義 TABLE_SECURITY_AGENTS 之數值。
    /// </summary>
public const string TABLE_SECURITY_AGENTS = @"
CREATE TABLE SecurityAgents(
    AgentId uniqueidentifier PRIMARY KEY NOT NULL,
    Name nvarchar(250) NOT NULL,
    AssemblyName nvarchar(250) NOT NULL,
    HardLockAttempts int NULL,
	HardLockTimeHours int NULL,
	LockForever bit NULL,
	SoftLockAttempts int NULL,
	SoftLockTimeMinutes int NULL,
    OverwriteConfiguration bit NOT NULL,
    DisplayName nvarchar(100) NOT NULL,
    Enabled bit NOT NULL DEFAULT 0,
    Serial int NOT NULL DEFAULT 0
)";

        /// <summary>
    /// 定義 TABLE_SECURITY_AGENT_CONFIG 之數值。
    /// </summary>
public const string TABLE_SECURITY_AGENT_CONFIG = @"
CREATE TABLE SecurityAgentConfig(
    AgentId uniqueidentifier not null,
    PropertyName nvarchar(255) not null,
    PropertyValueString nvarchar(255) null,
    PRIMARY KEY(AgentId, PropertyName)
)";

        /// <summary>
    /// 定義 TABLE_SECURITY_AGENT_CONFIG_CLUSTERED_KEY 之數值。
    /// </summary>
public const string TABLE_SECURITY_AGENT_CONFIG_CLUSTERED_KEY = @"
ALTER TABLE SecurityAgentConfig ADD CONSTRAINT
	PK_SecurityAgentConfig PRIMARY KEY
	(
	AgentId,
	PropertyName
	)
";

        /// <summary>
    /// 定義 TABLE_WHITE_LIST 之數值。
    /// </summary>
public const string TABLE_WHITE_LIST = @"
CREATE TABLE Whitelist(
    IPAddress nvarchar(80) not null,
    NetworkMask nvarchar(80) not null
)";
        /// <summary>
    /// 定義 TABLE_BLACKLIST_NETWORKS 之數值。
    /// </summary>
public const string TABLE_BLACKLIST_NETWORKS = @"
CREATE TABLE Blacklist(
    IPAddress nvarchar(80) not null,
    NetworkMask nvarchar(80) not null
)";

        /// <summary>
    /// 定義 TABLE_APP_CONFIG 之數值。
    /// </summary>
public const string TABLE_APP_CONFIG = @"
CREATE TABLE AppConfig(
    ConfigKey nvarchar(250) PRIMARY KEY not null,
    ConfigValue nvarchar(250) null)";


        /// <summary>
    /// 定義 TABLE_AGENT_STATISTICS 之數值。
    /// </summary>
public const string TABLE_AGENT_STATISTICS = @"
CREATE TABLE AgentStatistics(
    AgentId uniqueidentifier PRIMARY KEY NOT NULL,
    FailedLogins int not null default 0,
    HardLocks int not null default 0,
    SoftLocks int not null default 0)";
    /// <summary>
    /// 執行upgrade database作業。
    /// </summary>
    /// <param name="connection">connection參數。</param>
    public override void UpgradeDatabase(System.Data.IDbConnection connection)
    {
        try
        {
            RunCommand(connection, TABLE_DB_CONFIG);
            RunCommand(connection, TABLE_CONFIGURATION);
            RunCommand(connection, CREATE_DEFAULT_DB_CONFIGURATION);
            RunCommand(connection, CREATE_DEFAULT_CONFIGURATION);
            RunCommand(connection, TABLE_INTRUSION_LOG);
            RunCommand(connection, TABLE_LOCKS);
            RunCommand(connection, TABLE_SECURITY_AGENT_CONFIG);
            // RunCommand(connection, TABLE_SECURITY_AGENT_CONFIG_CLUSTERED_KEY);
            RunCommand(connection, TABLE_SECURITY_AGENTS);
            RunCommand(connection, TABLE_APP_CONFIG);
            RunCommand(connection, TABLE_WHITE_LIST);
            RunCommand(connection, TABLE_AGENT_STATISTICS);
        }
        catch (Exception)
        {
            throw;
        }
    }



}

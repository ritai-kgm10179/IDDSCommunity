using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Data;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表 IDDS 系統核心組態設定，提供設定值持久化、安全網路比對與預設值管理。
/// </summary>
public class IddsConfig
{
        /// <summary>
    /// 定義 DefaultSoftLockAttempts 之數值。
    /// </summary>
public const int DefaultSoftLockAttempts = 10;
        /// <summary>
    /// 定義 DefaultSoftLockMinutes 之數值。
    /// </summary>
public const int DefaultSoftLockMinutes = 1;
        /// <summary>
    /// 定義 DefaultHardLockAttempts 之數值。
    /// </summary>
public const int DefaultHardLockAttempts = 20;
        /// <summary>
    /// 定義 DefaultHardLockHours 之數值。
    /// </summary>
public const int DefaultHardLockHours = 1;
    /// <summary>
    /// 取得未設定郵件伺服器時顯示的標準 SMTP 連接埠。
    /// </summary>
    public const int DefaultSmtpPort = 25;
    private const long LocalAddressCacheLifetimeMilliseconds = 30000;
    private static readonly System.Threading.Lock LocalAddressLock = new();
    private static HashSet<IPAddress> localAddresses = [];
    private static long localAddressesRefreshedAt;
    private readonly Database database;
    private readonly HashSet<string> changedAppConfigKeys = [];

    static IddsConfig() =>
        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += static (_, _) => InvalidateLocalAddressCache();

        /// <summary>
    /// 定義 ENABLED_FEATURES_FREE 之數值。
    /// </summary>
public const int ENABLED_FEATURES_FREE = 1;
        /// <summary>
    /// 定義 ENABLED_FEATURES_PRO 之數值。
    /// </summary>
public const int ENABLED_FEATURES_PRO = 2;
        /// <summary>
    /// 定義 CONFIG_DB_VERSION_NUMBER 之數值。
    /// </summary>
public const int CONFIG_DB_VERSION_NUMBER = 1;
        /// <summary>
    /// 定義 LICENSE_FILE 之數值。
    /// </summary>
public const string LICENSE_FILE = "idds.vl";

        /// <summary>
    /// 定義 CONFIG_VALUE_IS_DEBUG 之數值。
    /// </summary>
public const string CONFIG_VALUE_IS_DEBUG = "Configuration.IsDebug";
        /// <summary>
    /// 定義 CONFIG_VALUE_LANGUAGE 之數值。
    /// </summary>
public const string CONFIG_VALUE_LANGUAGE = "Configuration.Language";
        /// <summary>
    /// 定義 CONFIG_VALUE_FIREWALL_BLOCK_MODE 之數值。
    /// </summary>
    public const string CONFIG_VALUE_FIREWALL_BLOCK_MODE = "Configuration.FirewallBlockMode";

    /// <summary>
    /// 定義 CONFIG_VALUE_ENABLE_CROSS_AGENT_CORRELATION 之數值。
    /// </summary>
    public const string CONFIG_VALUE_ENABLE_CROSS_AGENT_CORRELATION = "Configuration.EnableCrossAgentCorrelation";

    /// <summary>
    /// 定義 CONFIG_VALUE_CROSS_AGENT_SPRAY_ACCOUNT_THRESHOLD 之數值。
    /// </summary>
    public const string CONFIG_VALUE_CROSS_AGENT_SPRAY_ACCOUNT_THRESHOLD = "Configuration.CrossAgentSprayAccountThreshold";

    /// <summary>
    /// 定義 CONFIG_VALUE_CROSS_AGENT_SPRAY_IP_THRESHOLD 之數值。
    /// </summary>
    public const string CONFIG_VALUE_CROSS_AGENT_SPRAY_IP_THRESHOLD = "Configuration.CrossAgentSprayIpThreshold";

    /// <summary>
    /// 定義 CONFIG_VALUE_CROSS_AGENT_SLIDING_WINDOW_MINUTES 之數值。
    /// </summary>
    public const string CONFIG_VALUE_CROSS_AGENT_SLIDING_WINDOW_MINUTES = "Configuration.CrossAgentSlidingWindowMinutes";

    /// <summary>
    /// 定義跨來源語意去重容許秒數之設定鍵值。
    /// </summary>
    public const string CONFIG_VALUE_CROSS_AGENT_SEMANTIC_DEDUPLICATION_SECONDS = "Configuration.CrossAgentSemanticDeduplicationSeconds";

    /// <summary>
    /// 定義 CONFIG_VALUE_TRUSTED_PROXY_CIDRS 之數值。
    /// </summary>
    public const string CONFIG_VALUE_TRUSTED_PROXY_CIDRS = "Configuration.TrustedProxyCidrs";


    private const int IDDS_PRODUCT_ID = 0x66;
    // production server

    private static IddsConfig? _instance;
        /// <summary>
    /// 取得或設定 全域共用單例執行個體。
    /// </summary>
public static IddsConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new IddsConfig(Database.Instance);
                _instance.Load();
            }
            return _instance;
        }
    }

    private string? _pluginsDirectory;
        /// <summary>
    /// 取得或設定 PluginsDirectory。
    /// </summary>
public string PluginsDirectory
    {
        get => _pluginsDirectory ?? string.Empty;
        set
        {
            _pluginsDirectory = value;
            if (!string.IsNullOrEmpty(_pluginsDirectory) && !System.IO.Directory.Exists(_pluginsDirectory))
                System.IO.Directory.CreateDirectory(_pluginsDirectory);
        }
    }




    /// <summary>
    /// 儲存設定變更作業。
    /// </summary>
    public void Save()
    {
        if (!database.IsConfigured) configureDatabase();
        try
        {
            database.ExecuteNonQuery(@"insert into Configuration(ConfigVersionDate,
                    HardLockAttempts, HardLockTimeHours, LockForever, SoftLockAttempts, SoftLockTimeMinutes,
                    UseSafeNetworkList, PluginDirectory, LicenseKey, ActivationId, SendInfoMail,
                SmtpPort, SenderEmailAddress, SmtpRequiresAuthentication, NotificationEmailAddress, SmtpServer,
                SmtpUsername, SmtpPassword, CyberSheriffContributor, WebBasedMonitoring, HardwareId, SmtpSslRequired)
                values(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17,@p18,@p19,@p20,@p21)",
                DateTime.Now, HardLockAttempts, HardLockTimeHours,
                LockForever, SoftLockAttempts, SoftLockTimeMinutes, UseSafeNetworkList, PluginDirectory,
                DBNull.Value, DBNull.Value, SendInfoMail, SmtpPort, SenderEmailAddress, SmtpRequiresAuthentication,
                NotificationEmailAddress, SmtpServer, SmtpUsername, SmtpPassword, CyberSheriffContributor, WebBasedMonitoring, DBNull.Value, SmtpSslRequired);
            RecordConfigurationAudit(null, "Configuration");
            SaveAppConfig();
        }
        catch (Exception)
        {
            throw;
        }
    }
    /// <summary>
    /// Loads requested operation.
    /// </summary>
    public void Load()
    {
        if (!database.IsConfigured) configureDatabase();
        IDataReader? reader = null;
        try
        {
            reader = database.ExecuteReader("select * from Configuration order by ConfigVersionNumber desc LIMIT 1");
            if (reader.Read())
            {
                HardLockAttempts = Db.DbValueConverter.ToInt(reader["HardLockAttempts"]);
                HardLockTimeHours = Db.DbValueConverter.ToInt(reader["HardLockTimeHours"]);
                LockForever = Db.DbValueConverter.ToBool(reader["LockForever"]);
                SoftLockAttempts = Db.DbValueConverter.ToInt(reader["SoftLockAttempts"]);
                SoftLockTimeMinutes = Db.DbValueConverter.ToInt(reader["SoftLockTimeMinutes"]);
                UseSafeNetworkList = Db.DbValueConverter.ToBool(reader["UseSafeNetworkList"]);
                PluginDirectory = Db.DbValueConverter.ToString(reader["PluginDirectory"]);
                SendInfoMail = Db.DbValueConverter.ToBool(reader["SendInfoMail"]);
                SmtpPort = Db.DbValueConverter.ToInt(reader["SmtpPort"]);
                SenderEmailAddress = Db.DbValueConverter.ToString(reader["SenderEmailAddress"]);
                SmtpRequiresAuthentication = Db.DbValueConverter.ToBool(reader["SmtpRequiresAuthentication"]);
                NotificationEmailAddress = Db.DbValueConverter.ToString(reader["NotificationEmailAddress"]);
                SmtpServer = Db.DbValueConverter.ToString(reader["SmtpServer"]);
                SmtpUsername = Db.DbValueConverter.ToString(reader["SmtpUsername"]);
                SmtpPassword = Db.DbValueConverter.ToString(reader["SmtpPassword"]);
                CyberSheriffContributor = Db.DbValueConverter.ToBool(reader["CyberSheriffContributor"]);
                WebBasedMonitoring = Db.DbValueConverter.ToBool(reader["WebBasedMonitoring"]);
                SmtpSslRequired = Db.DbValueConverter.ToBool(reader["SmtpSslRequired"]);
                LoadSafeNetworks();
            }
            else
            {
                database.ExecuteNonQuery(Db.Version_2_1.CREATE_DEFAULT_CONFIGURATION);

            }

        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (reader != null && !reader.IsClosed) reader.Close();
        }
    }

    private readonly object _configLock = new();
    private Dictionary<string, string>? _appConfig;
        /// <summary>
    /// 取得或設定 AppConfig。
    /// </summary>
public Dictionary<string, string> AppConfig
    {
        get
        {
            lock (_configLock)
            {
                if (_appConfig == null)
                {
                    LoadAppConfig();
                }
                return _appConfig!;
            }
        }
    }
    /// <summary>
    /// Loads app config.
    /// </summary>
    public void LoadAppConfig()
    {
        lock (_configLock)
        {
            _appConfig = LoadConfig("AppConfig");
        }
    }
    /// <summary>
    /// Gets config value.
    /// </summary>
    /// <param name="key">key參數。</param>
    /// <returns>傳回get config value結果。</returns>
    public string GetConfigValue(string key)
    {
        lock (_configLock)
        {
            if (!AppConfig.ContainsKey(key)) AppConfig.Add(key, string.Empty);
            return AppConfig[key];
        }
    }
    /// <summary>
    /// Sets config value.
    /// </summary>
    /// <param name="key">key參數。</param>
    /// <param name="value">要處理的value。</param>
    public void SetConfigValue(string key, string value)
    {
        lock (_configLock)
        {
            if (AppConfig.ContainsKey(key))
            {
                AppConfig[key] = value;
            }
            else
            {
                AppConfig.Add(key, value);
            }
            changedAppConfigKeys.Add(key);
        }
    }
    /// <summary>
    /// Saves app config.
    /// </summary>
    public void SaveAppConfig()
    {
        if (!database.IsConfigured) configureDatabase();
        lock (_configLock)
        {
            database.ExecuteInTransaction((_, trans) =>
            {
                database.ExecuteNonQuery("delete from AppConfig", trans);
                foreach (string key in AppConfig.Keys)
                {
                    object? exists = database.ExecuteScalar("select count(*) from AppConfig where ConfigKey=@p0", trans, key);
                    if (exists != null && int.TryParse(exists.ToString(), out int count) && count > 0)
                    {
                        database.ExecuteNonQuery("UPDATE AppConfig SET ConfigValue = @p1 WHERE ConfigKey = @p0", trans, key, AppConfig[key]);
                    }
                    else
                    {
                        database.ExecuteNonQuery("insert into AppConfig(ConfigKey, ConfigValue) Values(@p0, @p1)", trans, key, AppConfig[key]);
                    }
                }
                foreach (string key in changedAppConfigKeys)
                    RecordConfigurationAudit(trans, key);
            });
            changedAppConfigKeys.Clear();
        }
    }
    /// <summary>
    /// Loads config.
    /// </summary>
    /// <param name="configTable">config table參數。</param>
    /// <returns>傳回load config結果。</returns>
    private Dictionary<string, string> LoadConfig(string configTable)
    {
        if (!database.IsConfigured)
        {
            try
            {
                configureDatabase();
            }
            catch
            {
                return [];
            }
        }
        Dictionary<string, string> config = [];
        try
        {
            using IDataReader rdr = database.ExecuteReader(string.Format("select ConfigKey, ConfigValue from {0}", configTable));
            while (rdr.Read())
            {
                config.Add(Db.DbValueConverter.ToString(rdr["ConfigKey"]), Db.DbValueConverter.ToString(rdr["ConfigValue"]));
            }
        }
        catch
        {
            // 在未設定資料庫或無權限之獨立/測試環境中維持記憶體字典運作
        }
        return config;
    }
    /// <summary>
    /// Loads safe networks.
    /// </summary>
    public void LoadSafeNetworks() => SafeNetworks = LoadNetworkList("WhiteList");
    /// <summary>
    /// Saves safe networks.
    /// </summary>
    public void SaveSafeNetworks()
    {
        if (!database.IsConfigured) configureDatabase();
        database.ExecuteInTransaction((_, trans) =>
        {
            database.ExecuteNonQuery("delete from WhiteList", trans);
            foreach (CSafeNetwork net in SafeNetworks)
            {
                database.ExecuteNonQuery("insert into WhiteList(IpAddress, NetworkMask) values (@p0, @p1)", trans, net.IpAddress, net.SubnetMask);
            }
            RecordConfigurationAudit(trans, "SafeNetworks");
        });
    }
    /// <summary>
    /// Records an atomic configuration-change audit event without persisting the setting value.
    /// </summary>
    /// <param name="transaction">The active configuration transaction, or <see langword="null"/> for an independent command.</param>
    /// <param name="subject">The stable configuration key or area.</param>
    private void RecordConfigurationAudit(IDbTransaction? transaction, string subject)
    {
        string actor = Environment.UserDomainName + "\\" + Environment.UserName;
        database.ExecuteNonQuery(
            "INSERT INTO ProtectionAuditLog(OccurredUtc, EventType, Outcome, Actor, Subject, Details) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
            transaction,
            TimeProvider.System.GetUtcNow().ToString("O"),
            "Configuration.Change",
            "Succeeded",
            actor,
            subject,
            string.Empty);
    }
    /// <summary>
    /// Loads network list.
    /// </summary>
    /// <param name="list">list參數。</param>
    /// <returns>傳回load network list結果。</returns>
    public CSafeNetworks LoadNetworkList(string list)
    {
        if (!database.IsConfigured) configureDatabase();
        if (!string.Equals(list, "WhiteList", StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(list), "Only the configured safe-network table can be loaded.");
        }
        CSafeNetworks net = [];
        using IDataReader rdr = database.ExecuteReader("Select IpAddress, NetworkMask from WhiteList");
        while (rdr.Read())
        {
            net.Add(new CSafeNetwork(Db.DbValueConverter.ToString(rdr["IpAddress"]), Db.DbValueConverter.ToString(rdr["NetworkMask"])));
        }
        return net;
    }

    /// <summary>
    /// 取得全系統共用之預設 SQLite 設定資料庫目錄。
    /// </summary>
    public static string GetDefaultDataDirectory()
    {
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return string.IsNullOrEmpty(commonAppData)
            ? AppDomain.CurrentDomain.BaseDirectory
            : ResolveDefaultDataDirectory(commonAppData);
    }

    internal static string ResolveDefaultDataDirectory(string commonAppData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commonAppData);
        string targetDir = System.IO.Path.Combine(commonAppData, "IDDS Community");
        string legacyDir = System.IO.Path.Combine(commonAppData, "IDDSCommunity");

        if (System.IO.Directory.Exists(legacyDir))
        {
            if (!System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.Move(legacyDir, targetDir);
            }
            else
            {
                bool targetHasEntries = System.IO.Directory.EnumerateFileSystemEntries(targetDir).Any();
                bool legacyHasEntries = System.IO.Directory.EnumerateFileSystemEntries(legacyDir).Any();
                if (!legacyHasEntries)
                {
                    System.IO.Directory.Delete(legacyDir);
                }
                else if (!targetHasEntries)
                {
                    System.IO.Directory.Delete(targetDir);
                    System.IO.Directory.Move(legacyDir, targetDir);
                }
                else
                {
                    throw new InvalidOperationException(
                        Localization.Strings.Format("Two populated data directories were detected. Automatic merging was refused to prevent separating the database and encryption key: '{0}', '{1}'.", targetDir, legacyDir));
                }
            }
        }

        System.IO.Directory.CreateDirectory(targetDir);
        return targetDir;
    }

    /// <summary>
    /// 執行configure database作業。
    /// </summary>
    internal void configureDatabase()
    {
        if (string.IsNullOrEmpty(ApplicationPath) || ApplicationPath == AppDomain.CurrentDomain.BaseDirectory)
        {
            ApplicationPath = GetDefaultDataDirectory();
        }
        database.Configure(ApplicationPath);
    }

        /// <summary>
    /// 取得或設定 應用程式主目錄路徑。
    /// </summary>
public string ApplicationPath { get; set; } = GetDefaultDataDirectory();

        /// <summary>
    /// 取得或設定 ConfigVersionNumber。
    /// </summary>
public int ConfigVersionNumber { get; set; }
        /// <summary>
    /// 取得或設定 Expires。
    /// </summary>
public DateTime? Expires { get; set; }
        /// <summary>
    /// 取得或設定 Edition。
    /// </summary>
public string Edition { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="IddsConfig"/> class的新執行個體。
    /// </summary>
    public IddsConfig(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        this.database = database;
    }


    /*
     *
     * Configuration values for IDDS plus Security Agents
     *
     *
     */

        /// <summary>
    /// 取得或設定 硬封鎖失敗次數門檻。
    /// </summary>
public int HardLockAttempts { get; set; }
        /// <summary>
    /// 取得或設定 硬封鎖持續時數。
    /// </summary>
public int HardLockTimeHours { get; set; }
        /// <summary>
    /// 取得或設定 是否永久封鎖。
    /// </summary>
public bool LockForever { get; set; }
        /// <summary>
    /// 取得或設定 軟封鎖失敗次數門檻。
    /// </summary>
public int SoftLockAttempts { get; set; }
        /// <summary>
    /// 取得或設定 軟封鎖持續分鐘數。
    /// </summary>
public int SoftLockTimeMinutes { get; set; }
        /// <summary>
    /// 取得或設定 是否使用安全網路清單。
    /// </summary>
public bool UseSafeNetworkList { get; set; }
        /// <summary>
    /// 取得或設定 PluginDirectory。
    /// </summary>
public static string PluginDirectory { get; set; } = string.Empty;

    /* private string _hardwareId;
    private string GetHardwareId() {
        if (String.IsNullOrEmpty(_hardwareId)) {

            _hardwareId = KeyHelper.GetCurrentHardwareId();
        }
        return _hardwareId;
    } */


        /// <summary>
    /// 取得或設定 CyberSheriffContributor。
    /// </summary>
public bool CyberSheriffContributor { get; set; }

    private CSafeNetworks? _safeNetworks;
        /// <summary>
    /// 取得或設定 SafeNetworks。
    /// </summary>
public CSafeNetworks SafeNetworks
    {
        get
        {
            _safeNetworks ??= SafeNetworks = LoadNetworkList("WhiteList");
            return _safeNetworks;
        }

        set => _safeNetworks = value;
    }
    /// <summary>
    /// Gets default configuration.
    /// </summary>
    /// <returns>傳回get default configuration結果。</returns>
    public static IddsConfig GetDefaultConfiguration()
    {
        IddsConfig config = new(Database.Instance)
        {
            HardLockAttempts = DefaultHardLockAttempts,
            SoftLockAttempts = DefaultSoftLockAttempts,
            HardLockTimeHours = DefaultHardLockHours,
            SoftLockTimeMinutes = DefaultSoftLockMinutes,
            LockForever = false,
            UseSafeNetworkList = false,
            SafeNetworks = [],
            SmtpPort = DefaultSmtpPort,
            SmtpSslRequired = false,
            SmtpRequiresAuthentication = false,
            SendInfoMail = false,
            SenderEmailAddress = "IDDSCommunity.IDDS@" + Dns.GetHostEntry("localhost").HostName
        };
        //config.AgentConfigurations.GetAgentConfig(PluginDirectory + "IDDSCommunity.IntrusionDetection.Base.Plugins.dll", "WindowsSecurityBase");
        //config.AgentConfigurations[0].Enabled = true;
        return config;
    }

    /// <summary>
    /// 代表安全網路規則項目之集合清單。
    /// </summary>
    public class CSafeNetworks : List<CSafeNetwork> { }

    /// <summary>
    /// 代表單一安全網路 IP、CIDR 子網路或動態 DNS (DDNS FQDN) 規則項目。
    /// </summary>
    public class CSafeNetwork
    {
        /// <summary>
        /// 取得或設定 IP 位址、CIDR 或動態主機名稱 (FQDN)。
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 取得或設定 子網路遮罩（若為 FQDN 則可為空白）。
        /// </summary>
        public string SubnetMask { get; set; } = string.Empty;

        /// <summary>
        /// 取得或設定 本地化顯示名稱。
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(SubnetMask) ? IpAddress : string.Format("{0}/{1}", IpAddress, SubnetMask);

        /// <summary>
        /// 初始化 <see cref="CSafeNetwork"/> class的新執行個體。
        /// </summary>
        public CSafeNetwork()
        {
        }

        /// <summary>
        /// 初始化 <see cref="CSafeNetwork"/> class的新執行個體。
        /// </summary>
        /// <param name="ipAddress">ip address參數。</param>
        /// <param name="subnetmask">subnetmask參數。</param>
        public CSafeNetwork(string ipAddress, string subnetmask)
        {
            IpAddress = ipAddress;
            SubnetMask = subnetmask;
        }
    }

    /// <summary>
    /// 取得或設定 威脅情資叢集主機角色（獨立單機、邊緣節點、威脅中繼中心）。
    /// </summary>
    public ThreatIntelligence.ThreatHubRole ThreatHubRole
    {
        get => Enum.TryParse(GetConfigValue("ThreatHubRole"), out ThreatIntelligence.ThreatHubRole role) ? role : ThreatIntelligence.ThreatHubRole.Standalone;
        set => SetConfigValue("ThreatHubRole", value.ToString());
    }

    /// <summary>
    /// 取得或設定 威脅情資中繼中心（Threat Hub）伺服器端點 URL。
    /// </summary>
    public string ThreatHubEndpoint
    {
        get => GetConfigValue("ThreatHubEndpoint");
        set => SetConfigValue("ThreatHubEndpoint", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 威脅情資叢集 API 金鑰。
    /// </summary>
    public string ThreatHubApiKey
    {
        get
        {
            string key = GetConfigValue("ThreatHubApiKey");
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString("N");
                SetConfigValue("ThreatHubApiKey", key);
            }
            return key;
        }
        set => SetConfigValue("ThreatHubApiKey", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 Threat Hub 內建服務監聽之連接埠（預設 8443）。
    /// </summary>
    public int ThreatHubPort
    {
        get => int.TryParse(GetConfigValue("ThreatHubPort"), out int port) && port > 0 ? port : 8443;
        set => SetConfigValue("ThreatHubPort", value.ToString());
    }

    /// <summary>
    /// 取得或設定 邊緣節點與 Threat Hub 同步威脅情資之間隔秒數（預設 60 秒）。
    /// </summary>
    public int ThreatHubSyncIntervalSeconds
    {
        get => int.TryParse(GetConfigValue("ThreatHubSyncIntervalSeconds"), out int s) && s > 0 ? s : 60;
        set => SetConfigValue("ThreatHubSyncIntervalSeconds", value.ToString());
    }

    /// <summary>
    /// 取得或設定 永久硬封鎖在無攻擊活動後自動轉移至假釋觀察期之天數（預設 90 天）。
    /// </summary>
    public int ProbationDecayDays
    {
        get => int.TryParse(GetConfigValue("ProbationDecayDays"), out int d) && d > 0 ? d : 90;
        set => SetConfigValue("ProbationDecayDays", value.ToString());
    }

    /// <summary>
    /// 取得或設定 動態 DNS (DDNS FQDN) 安全網路解析更新頻率分鐘數（預設 5 分鐘）。
    /// </summary>
    public int DynamicDnsIntervalMinutes
    {
        get => int.TryParse(GetConfigValue("DynamicDnsIntervalMinutes"), out int m) && m > 0 ? m : 5;
        set => SetConfigValue("DynamicDnsIntervalMinutes", value.ToString());
    }

    /// <summary>
    /// 取得或設定 是否啟用外部威脅情報（Threat Feeds）自動訂閱與主動防護（預設 false）。
    /// </summary>
    public bool EnableExternalThreatFeeds
    {
        get => bool.TryParse(GetConfigValue("EnableExternalThreatFeeds"), out bool b) && b;
        set => SetConfigValue("EnableExternalThreatFeeds", value.ToString());
    }

    /// <summary>
    /// 取得或設定 外部威脅情報更新週期小時數（預設 24 小時）。
    /// </summary>
    public int ThreatFeedUpdateIntervalHours
    {
        get => int.TryParse(GetConfigValue("ThreatFeedUpdateIntervalHours"), out int h) && h > 0 ? h : 24;
        set => SetConfigValue("ThreatFeedUpdateIntervalHours", value.ToString());
    }

    /// <summary>
    /// 取得或設定 IPsum 情資最低採納等級門檻（預設 3，代表至少被 3 個獨立組織標記為惡意）。
    /// </summary>
    public int ThreatFeedMinLevel
    {
        get => int.TryParse(GetConfigValue("ThreatFeedMinLevel"), out int l) && l > 0 ? l : 3;
        set => SetConfigValue("ThreatFeedMinLevel", value.ToString());
    }

    /// <summary>
    /// 取得或設定 外部情資惡意 IP 預設存活天數（TTL，預設 7 天）。
    /// </summary>
    public int ThreatFeedTtlDays
    {
        get => int.TryParse(GetConfigValue("ThreatFeedTtlDays"), out int d) && d > 0 ? d : 7;
        set => SetConfigValue("ThreatFeedTtlDays", value.ToString());
    }

    /// <summary>
    /// 取得或設定 自訂外部威脅情資 URL 清單（以換行或分號分隔）。
    /// </summary>
    public string ThreatFeedCustomUrls
    {
        get => GetConfigValue("ThreatFeedCustomUrls");
        set => SetConfigValue("ThreatFeedCustomUrls", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 AbuseIPDB API 金鑰（留空表示未啟用 AbuseIPDB 訂閱）。
    /// </summary>
    public string AbuseIpDbApiKey
    {
        get => GetConfigValue("AbuseIpDbApiKey");
        set => SetConfigValue("AbuseIpDbApiKey", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 AbuseIPDB 惡意信心度門檻百分比（預設 90%）。
    /// </summary>
    public int AbuseIpDbMinConfidence
    {
        get => int.TryParse(GetConfigValue("AbuseIpDbMinConfidence"), out int c) && c > 0 ? c : 90;
        set => SetConfigValue("AbuseIpDbMinConfidence", value.ToString());
    }

    /// <summary>
    /// 取得或設定 是否啟用動態 Bogon (Team Cymru Fullbogons) 前綴自動更新（預設 true）。
    /// </summary>
    public bool EnableDynamicBogonUpdate
    {
        get => bool.TryParse(GetConfigValue("EnableDynamicBogonUpdate"), out bool b) ? b : true;
        set => SetConfigValue("EnableDynamicBogonUpdate", value.ToString());
    }

    /// <summary>
    /// 取得或設定 動態 Bogon (Fullbogons IPv4) 清單下載 URL。
    /// </summary>
    public string DynamicBogonIpv4Url
    {
        get
        {
            string url = GetConfigValue("DynamicBogonIpv4Url");
            return string.IsNullOrWhiteSpace(url) ? "https://www.team-cymru.org/Services/Bogons/fullbogons-ipv4.txt" : url;
        }
        set => SetConfigValue("DynamicBogonIpv4Url", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 動態 Bogon (Fullbogons IPv6) 清單下載 URL。
    /// </summary>
    public string DynamicBogonIpv6Url
    {
        get
        {
            string url = GetConfigValue("DynamicBogonIpv6Url");
            return string.IsNullOrWhiteSpace(url) ? "https://www.team-cymru.org/Services/Bogons/fullbogons-ipv6.txt" : url;
        }
        set => SetConfigValue("DynamicBogonIpv6Url", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 是否啟用 GeoIP 資料庫定期自動更新。
    /// </summary>
    public bool EnableGeoIpAutoUpdate
    {
        get => bool.TryParse(GetConfigValue("EnableGeoIpAutoUpdate"), out bool b) ? b : true;
        set => SetConfigValue("EnableGeoIpAutoUpdate", value.ToString());
    }

    /// <summary>
    /// 取得或設定 GeoIP (IPv4) 資料庫下載 URL。
    /// </summary>
    public string GeoIpDatabaseIpv4Url
    {
        get
        {
            string url = GetConfigValue("GeoIpDatabaseIpv4Url");
            return string.IsNullOrWhiteSpace(url) ? "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv4.csv" : url;
        }
        set => SetConfigValue("GeoIpDatabaseIpv4Url", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 GeoIP (IPv6) 資料庫下載 URL。
    /// </summary>
    public string GeoIpDatabaseIpv6Url
    {
        get
        {
            string url = GetConfigValue("GeoIpDatabaseIpv6Url");
            return string.IsNullOrWhiteSpace(url) ? "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv6.csv" : url;
        }
        set => SetConfigValue("GeoIpDatabaseIpv6Url", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 本機自訂離線 GeoIP CSV 檔案路徑（選用）。
    /// </summary>
    public string GeoIpLocalFilePath
    {
        get => GetConfigValue("GeoIpLocalFilePath") ?? string.Empty;
        set => SetConfigValue("GeoIpLocalFilePath", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 GeoIP 資料庫自動更新週期天數（預設 7 天）。
    /// </summary>
    public int GeoIpUpdateIntervalDays
    {
        get => int.TryParse(GetConfigValue("GeoIpUpdateIntervalDays"), out int v) ? Math.Clamp(v, 1, 365) : 7;
        set => SetConfigValue("GeoIpUpdateIntervalDays", Math.Clamp(value, 1, 365).ToString());
    }

    /// <summary>
    /// 取得或設定 是否啟用國家/地區地理封鎖 (Geo-blocking)。
    /// </summary>
    public bool EnableGeoBlocking
    {
        get => bool.TryParse(GetConfigValue("EnableGeoBlocking"), out bool b) && b;
        set => SetConfigValue("EnableGeoBlocking", value.ToString());
    }

    /// <summary>
    /// 取得或設定 封鎖的國家 ISO 3166-1 代碼清單（逗號分隔，例如 CN,RU,KP）。
    /// </summary>
    public string BlockedCountryCodes
    {
        get => GetConfigValue("BlockedCountryCodes") ?? string.Empty;
        set => SetConfigValue("BlockedCountryCodes", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 是否啟用雲端邊界 WAF / NSG / 電信雲防火牆聯動。
    /// </summary>
    public bool EnableCloudPerimeter
    {
        get => bool.TryParse(GetConfigValue("EnableCloudPerimeter"), out bool b) && b;
        set => SetConfigValue("EnableCloudPerimeter", value.ToString());
    }

    /// <summary>
    /// 取得或設定 雲端邊界提供者類型。
    /// </summary>
    public CloudPerimeter.CloudPerimeterType CloudPerimeterType
    {
        get => Enum.TryParse(GetConfigValue("CloudPerimeterType"), out CloudPerimeter.CloudPerimeterType t) ? t : CloudPerimeter.CloudPerimeterType.None;
        set => SetConfigValue("CloudPerimeterType", value.ToString());
    }

    /// <summary>
    /// 取得或設定 雲端邊界 API 金鑰 / Token。
    /// </summary>
    public string CloudPerimeterApiKey
    {
        get => GetConfigValue("CloudPerimeterApiKey") ?? string.Empty;
        set => SetConfigValue("CloudPerimeterApiKey", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 雲端邊界端點 URL。
    /// </summary>
    public string CloudPerimeterEndpoint
    {
        get => GetConfigValue("CloudPerimeterEndpoint") ?? string.Empty;
        set => SetConfigValue("CloudPerimeterEndpoint", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 雲端邊界資源識別碼 (Security Group ID / WAF IPSet ID 等)。
    /// </summary>
    public string CloudPerimeterResourceId
    {
        get => GetConfigValue("CloudPerimeterResourceId") ?? string.Empty;
        set => SetConfigValue("CloudPerimeterResourceId", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 雲端邊界次要識別碼 (Region / Subscription ID / Project ID / Zone ID)。
    /// </summary>
    public string CloudPerimeterSecondaryId
    {
        get => GetConfigValue("CloudPerimeterSecondaryId") ?? string.Empty;
        set => SetConfigValue("CloudPerimeterSecondaryId", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 雲端邊界第三識別碼 (Azure Resource Group 等)。
    /// </summary>
    public string CloudPerimeterTertiaryId
    {
        get => GetConfigValue("CloudPerimeterTertiaryId") ?? string.Empty;
        set => SetConfigValue("CloudPerimeterTertiaryId", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 是否啟用合法使用者自助驗證解鎖門戶。
    /// </summary>
    public bool EnableSelfServicePortal
    {
        get => bool.TryParse(GetConfigValue("EnableSelfServicePortal"), out bool b) && b;
        set => SetConfigValue("EnableSelfServicePortal", value.ToString());
    }

    /// <summary>
    /// 取得或設定 自助解鎖門戶 HTTP 監聽連接埠 (預設 8444)。
    /// </summary>
    public int SelfServicePortalPort
    {
        get => int.TryParse(GetConfigValue("SelfServicePortalPort"), out int p) && p > 0 ? p : 8444;
        set => SetConfigValue("SelfServicePortalPort", value.ToString());
    }

    /// <summary>
    /// 取得或設定 自助解鎖門戶監聽 IP 位址 (預設 "0.0.0.0")。
    /// </summary>
    public string SelfServicePortalListenIp
    {
        get
        {
            string ip = GetConfigValue("SelfServicePortalListenIp");
            return string.IsNullOrWhiteSpace(ip) ? "0.0.0.0" : ip;
        }
        set => SetConfigValue("SelfServicePortalListenIp", value ?? "0.0.0.0");
    }

    /// <summary>
    /// 取得或設定 自助門戶 RFC 6238 TOTP 共享 Base32 密鑰。
    /// </summary>
    public string SelfServiceTotpSecret
    {
        get => GetConfigValue("SelfServiceTotpSecret") ?? string.Empty;
        set => SetConfigValue("SelfServiceTotpSecret", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 是否啟用長週期慢速隱蔽探測 (Slow &amp; Low) 機器學習異常分析。
    /// </summary>
    public bool EnableSlowAndLowDetection
    {
        get => bool.TryParse(GetConfigValue("EnableSlowAndLowDetection"), out bool b) ? b : true;
        set => SetConfigValue("EnableSlowAndLowDetection", value.ToString());
    }

    /// <summary>
    /// 取得或設定 慢速隱蔽探測觸發封鎖之異常分數門檻 (預設 8.0)。
    /// </summary>
    public double SlowAndLowAnomalyThreshold
    {
        get => double.TryParse(GetConfigValue("SlowAndLowAnomalyThreshold"), out double t) && t > 0 ? t : 8.0;
        set => SetConfigValue("SlowAndLowAnomalyThreshold", value.ToString());
    }

    /// <summary>
    /// 取得或設定 是否啟用誘餌帳號 (Honey-Accounts) 欺敵陷阱防護。
    /// </summary>
    public bool EnableHoneyAccounts
    {
        get => bool.TryParse(GetConfigValue("EnableHoneyAccounts"), out bool b) ? b : true;
        set => SetConfigValue("EnableHoneyAccounts", value.ToString());
    }

    /// <summary>
    /// 取得或設定 誘餌帳號清單（以逗號或分號分隔，如 admin_backup, test_vpn, sql_svc）。
    /// </summary>
    public string HoneyAccounts
    {
        get => GetConfigValue("HoneyAccounts") ?? "admin_backup,root_admin,test_vpn,sql_svc";
        set => SetConfigValue("HoneyAccounts", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 是否啟用安全 RESTful Management API。
    /// </summary>
    public bool EnableManagementApi
    {
        get => bool.TryParse(GetConfigValue("EnableManagementApi"), out bool b) ? b : false;
        set => SetConfigValue("EnableManagementApi", value.ToString());
    }

    /// <summary>
    /// 取得或設定 RESTful Management API 監聽連接埠 (預設 8443)。
    /// </summary>
    public int ManagementApiPort
    {
        get => int.TryParse(GetConfigValue("ManagementApiPort"), out int p) && p > 0 ? p : 8443;
        set => SetConfigValue("ManagementApiPort", value.ToString());
    }

    /// <summary>
    /// 取得或設定 RESTful Management API 金鑰 (X-Api-Key)。
    /// </summary>
    public string ManagementApiKey
    {
        get => GetConfigValue("ManagementApiKey") ?? string.Empty;
        set => SetConfigValue("ManagementApiKey", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 自訂 SOAR 處置 PowerShell / CMD 腳本路徑。
    /// </summary>
    public string SoarRemediationScriptPath
    {
        get => GetConfigValue("SoarRemediationScriptPath") ?? string.Empty;
        set => SetConfigValue("SoarRemediationScriptPath", value ?? string.Empty);
    }

    /// <summary>
    /// 取得或設定 資料庫後端引擎型別 (0: SQLite, 1: PostgreSQL, 2: SqlServer)。
    /// </summary>
    public Storage.DatabaseBackendType DatabaseBackend
    {
        get => Enum.TryParse(GetConfigValue("DatabaseBackend"), out Storage.DatabaseBackendType b) ? b : Storage.DatabaseBackendType.SQLite;
        set => SetConfigValue("DatabaseBackend", value.ToString());
    }

        /// <summary>
    /// 取得或設定 SendInfoMail。
    /// </summary>
public bool SendInfoMail { get; set; }

        /// <summary>
    /// 取得或設定 通知收件者電子郵件地址。
    /// </summary>
public string NotificationEmailAddress { get; set; } = string.Empty;

        /// <summary>
    /// 取得或設定 SMTP 伺服器位址。
    /// </summary>
public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
    /// 取得或設定 SMTP 連接埠。
    /// </summary>
public int SmtpPort { get; set; }

        /// <summary>
    /// 取得或設定 SMTP 帳號名稱。
    /// </summary>
public string SmtpUsername { get; set; } = string.Empty;

        /// <summary>
    /// 取得或設定 SMTP 是否要求 SSL。
    /// </summary>
public bool SmtpSslRequired { get; set; }

    private string _smtpPassword = string.Empty;

        /// <summary>
    /// 取得或設定 SMTP 密碼。
    /// </summary>
public string SmtpPassword
    {
        get => _smtpPassword; set => _smtpPassword = value;
    }

        /// <summary>
    /// 取得或設定 寄件者電子郵件地址。
    /// </summary>
public string SenderEmailAddress { get; set; } = string.Empty;

        /// <summary>
    /// 取得或設定 SmtpRequiresAuthentication。
    /// </summary>
public bool SmtpRequiresAuthentication { get; set; }

        /// <summary>
    /// 取得或設定 WebBasedMonitoring。
    /// </summary>
public bool WebBasedMonitoring { get; set; }

    private bool? _isDebug;
        /// <summary>
    /// 取得或設定 IsDebug。
    /// </summary>
public bool IsDebug
    {
        get
        {
            if (!_isDebug.HasValue)
            {
                if (bool.TryParse(GetConfigValue(CONFIG_VALUE_IS_DEBUG), out bool isDebug))
                {
                    _isDebug = isDebug;
                }
                else
                {
                    _isDebug = false;
                }
            }
            return _isDebug.Value;
        }
        set
        {
            _isDebug = value;
            SetConfigValue(CONFIG_VALUE_IS_DEBUG, value.ToString());
        }
    }

        /// <summary>
    /// 取得或設定 Language。
    /// </summary>
public string Language
    {
        get => GetConfigValue(CONFIG_VALUE_LANGUAGE);
        set
        {
            SetConfigValue(CONFIG_VALUE_LANGUAGE, value);
            Localization.LanguageManager.Instance.Initialize(value);
        }
    }
    /// <summary>
    /// 取得或設定 supported Windows Firewall blocking mode. Invalid or obsolete values fail closed to inbound blocking.
    /// </summary>
    public FirewallBlockMode FirewallBlockMode
    {
        get => Enum.TryParse(GetConfigValue(CONFIG_VALUE_FIREWALL_BLOCK_MODE), true, out FirewallBlockMode mode)
            && Enum.IsDefined(mode) ? mode : FirewallBlockMode.Inbound;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            SetConfigValue(CONFIG_VALUE_FIREWALL_BLOCK_MODE, value.ToString());
        }
    }

    /// <summary>
    /// 取得或設定是否啟用跨代理程式攻擊關聯與密碼噴灑偵測功能（預設為 false）。
    /// </summary>
    public bool EnableCrossAgentCorrelation
    {
        get => bool.TryParse(GetConfigValue(CONFIG_VALUE_ENABLE_CROSS_AGENT_CORRELATION), out bool enabled) && enabled;
        set => SetConfigValue(CONFIG_VALUE_ENABLE_CROSS_AGENT_CORRELATION, value.ToString());
    }

    /// <summary>
    /// 取得或設定跨代理程式單一 IP 嘗試相異帳號之密碼噴灑門檻值（預設為 5）。
    /// </summary>
    public int CrossAgentSprayAccountThreshold
    {
        get => int.TryParse(GetConfigValue(CONFIG_VALUE_CROSS_AGENT_SPRAY_ACCOUNT_THRESHOLD), out int threshold) && threshold > 0 ? threshold : 5;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            SetConfigValue(CONFIG_VALUE_CROSS_AGENT_SPRAY_ACCOUNT_THRESHOLD, value.ToString());
        }
    }

    /// <summary>
    /// 取得或設定跨代理程式多個分散 IP 嘗試單一帳號之分散式密碼噴灑門檻值（預設為 5）。
    /// </summary>
    public int CrossAgentSprayIpThreshold
    {
        get => int.TryParse(GetConfigValue(CONFIG_VALUE_CROSS_AGENT_SPRAY_IP_THRESHOLD), out int threshold) && threshold > 0 ? threshold : 5;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            SetConfigValue(CONFIG_VALUE_CROSS_AGENT_SPRAY_IP_THRESHOLD, value.ToString());
        }
    }

    /// <summary>
    /// 取得或設定跨代理程式關聯與密碼噴灑之滑動時間窗分鐘數（預設為 10 分鐘）。
    /// </summary>
    public int CrossAgentSlidingWindowMinutes
    {
        get => int.TryParse(GetConfigValue(CONFIG_VALUE_CROSS_AGENT_SLIDING_WINDOW_MINUTES), out int minutes) && minutes > 0 ? minutes : 10;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            SetConfigValue(CONFIG_VALUE_CROSS_AGENT_SLIDING_WINDOW_MINUTES, value.ToString());
        }
    }

    /// <summary>
    /// 取得或設定缺少明確活動識別碼時，跨來源驗證事件允許的發生時間差秒數（預設為 15 秒）。
    /// </summary>
    public int CrossAgentSemanticDeduplicationSeconds
    {
        get => int.TryParse(GetConfigValue(CONFIG_VALUE_CROSS_AGENT_SEMANTIC_DEDUPLICATION_SECONDS), out int seconds)
            && seconds is >= 1 and <= 300 ? seconds : 15;
        set
        {
            if (value is < 1 or > 300)
                throw new ArgumentOutOfRangeException(nameof(value));
            SetConfigValue(CONFIG_VALUE_CROSS_AGENT_SEMANTIC_DEDUPLICATION_SECONDS, value.ToString());
        }
    }

    /// <summary>
    /// 取得或設定受信任反向代理 CIDR 清單字串（以逗號或分號分隔）。
    /// </summary>
    public string TrustedProxyCidrs
    {
        get => GetConfigValue(CONFIG_VALUE_TRUSTED_PROXY_CIDRS) ?? string.Empty;
        set => SetConfigValue(CONFIG_VALUE_TRUSTED_PROXY_CIDRS, value ?? string.Empty);
    }

    /// <summary>
    /// 取得解析後的受信任反向代理 IP 與 CIDR 區段清單。
    /// </summary>
    /// <returns>受信任反向代理字串集合。</returns>
    public IEnumerable<string> GetTrustedProxyList()
    {
        string raw = TrustedProxyCidrs;
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Gets smtp password.
    /// </summary>
    /// <returns>傳回get smtp password結果。</returns>
    public string GetSmtpPassword()
    {
        string smtpPassword = string.Empty;
        if (!string.IsNullOrEmpty(SmtpPassword))
        {
            smtpPassword = CryptoHelper.Decrypt(SmtpPassword, true);
            if (!CryptoHelper.IsCurrentFormat(SmtpPassword))
            {
                SetSmtpPassword(smtpPassword);
            }
        }
        return smtpPassword;
    }
    /// <summary>
    /// Determines whether in safe network.
    /// </summary>
    /// <param name="ipAddress">ip address參數。</param>
    /// <returns>若in safe network傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsInSafeNetwork(string ipAddress)
    {
        bool result = false;
        try
        {
            IPAddress address = IpAddressCanonicalizer.Canonicalize(IPAddress.Parse(ipAddress));
            foreach (CSafeNetwork net in SafeNetworks)
            {
                try
                {
                    if (ThreatIntelligence.DynamicDnsCache.IsIpInDdns(address, net.IpAddress))
                    {
                        return true;
                    }

                    if (IPAddress.TryParse(net.IpAddress, out IPAddress? rawNetAddress))
                    {
                        IPAddress networkAddress = IpAddressCanonicalizer.Canonicalize(rawNetAddress);
                        if (networkAddress.AddressFamily.Equals(address.AddressFamily))
                        {
                            switch (address.AddressFamily)
                            {
                                case System.Net.Sockets.AddressFamily.InterNetwork:
                                    result = IsIp4InNetwork(address, networkAddress, net.SubnetMask);
                                    break;
                                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                                    result = IsIp6InNetwork(address, networkAddress, int.Parse(net.SubnetMask));
                                    break;
                            }
                        }
                        if (result) return true;
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceWarning("Invalid safe-network entry {0}/{1}: {2}", net.IpAddress, net.SubnetMask, exception.Message);
                }
            }

        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceWarning("Invalid address supplied to safe-network evaluation: {0}", exception.Message);
        }
        return false;

    }

    /// <summary>
    /// Determines whether ip4 in network.
    /// </summary>
    /// <param name="address">address參數。</param>
    /// <param name="networkAddress">network address參數。</param>
    /// <param name="subnetMask">subnet mask參數。</param>
    /// <returns>若ip4 in network傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsIp4InNetwork(IPAddress address, IPAddress networkAddress, string subnetMask) => IsIpInNetwork(address, networkAddress, GetSubnetMaskBits(subnetMask), 4);
    /// <summary>
    /// Determines whether ip in network.
    /// </summary>
    /// <param name="address">address參數。</param>
    /// <param name="networkAddress">network address參數。</param>
    /// <param name="maskBits">mask bits參數。</param>
    /// <param name="addressLength">address length參數。</param>
    /// <returns>若ip in network傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsIpInNetwork(IPAddress address, IPAddress networkAddress, int maskBits, int addressLength)
    {
        byte[] addressBytes = address.GetAddressBytes();
        byte[] networkBytes = networkAddress.GetAddressBytes();
        if (addressBytes.Length != addressLength || networkBytes.Length != addressLength)
        {
            return false;
        }
        if (maskBits < 0 || maskBits > addressLength * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(maskBits));
        }

        int fullBytes = maskBits / 8;
        int remainingBits = maskBits % 8;
        if (!addressBytes.AsSpan(0, fullBytes).SequenceEqual(networkBytes.AsSpan(0, fullBytes))) return false;
        if (remainingBits == 0) return true;

        int mask = 0xFF << (8 - remainingBits);
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }

    /// <summary>
    /// Determines whether ip address local.
    /// </summary>
    /// <param name="address">address參數。</param>
    /// <returns>若ip address local傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsIpAddressLocal(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        IPAddress normalized = IpAddressCanonicalizer.Canonicalize(address);
        if (IPAddress.IsLoopback(normalized))
        {
            return true;
        }

        lock (LocalAddressLock)
        {
            long now = Environment.TickCount64;
            if (localAddresses.Count == 0 || now - localAddressesRefreshedAt >= LocalAddressCacheLifetimeMilliseconds)
            {
                HashSet<IPAddress> refreshed = [];
                foreach (System.Net.NetworkInformation.NetworkInterface iface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    System.Net.NetworkInformation.IPInterfaceProperties iprop = iface.GetIPProperties();
                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation info in iprop.UnicastAddresses)
                    {
                        IPAddress candidate = IpAddressCanonicalizer.Canonicalize(info.Address);
                        refreshed.Add(candidate);
                    }
                }

                localAddresses = refreshed;
                localAddressesRefreshedAt = now;
            }

            return localAddresses.Contains(normalized);
        }
    }

    private static void InvalidateLocalAddressCache()
    {
        lock (LocalAddressLock)
        {
            localAddresses = [];
            localAddressesRefreshedAt = 0;
        }
    }

    /// <summary>
    /// Gets subnet mask bits.
    /// </summary>
    /// <param name="subnetMask">subnet mask參數。</param>
    /// <returns>傳回get subnet mask bits結果。</returns>
    public static int GetSubnetMaskBits(string subnetMask)
    {
        if (int.TryParse(subnetMask, out int prefixLength) && prefixLength is >= 0 and <= 32) return prefixLength;
        if (!IPAddress.TryParse(subnetMask, out IPAddress? maskAddress) || maskAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("No valid subnet mask entered."), nameof(subnetMask));

        int result = 0;
        bool foundZero = false;
        foreach (byte maskByte in maskAddress.GetAddressBytes())
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                bool isSet = (maskByte & (1 << bit)) != 0;
                if (isSet)
                {
                    if (foundZero) throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Subnet mask bits must be contiguous."), nameof(subnetMask));
                    result++;
                }
                else
                {
                    foundZero = true;
                }
            }
        }
        return result;
    }
    /// <summary>
    /// Determines whether ip6 in network.
    /// </summary>
    /// <param name="address">address參數。</param>
    /// <param name="networkAddress">network address參數。</param>
    /// <param name="subnetMask">subnet mask參數。</param>
    /// <returns>若ip6 in network傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsIp6InNetwork(IPAddress address, IPAddress networkAddress, int subnetMask) => IsIpInNetwork(address, networkAddress, subnetMask, 16);

    /// <summary>
    /// Determines whether valid ip address.
    /// </summary>
    /// <param name="ipAddress">ip address參數。</param>
    /// <returns>若valid ip address傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsValidIpAddress(string ipAddress) => IPAddress.TryParse(ipAddress, out IPAddress? validIpAddress);
    /// <summary>
    /// Determines whether valid subnet mask.
    /// </summary>
    /// <param name="subnetMask">subnet mask參數。</param>
    /// <returns>若valid subnet mask傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsValidSubnetMask(string subnetMask)
    {
        try
        {
            _ = GetSubnetMaskBits(subnetMask);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    /// <summary>
    /// Converts string to ip address network.
    /// </summary>
    /// <param name="ipAddressNetwork">ip address network參數。</param>
    /// <returns>傳回convert string to ip address network結果。</returns>
    public static string ConvertStringToIpAddressNetwork(string ipAddressNetwork)
    {
        ipAddressNetwork = ipAddressNetwork.Trim();
        string[] parts = ipAddressNetwork.Split('/');
        if (parts.Length is < 1 or > 2 || !IPAddress.TryParse(parts[0], out IPAddress? address))
            throw new ArgumentException(nameof(NetworkInputError.InvalidIpAddress), nameof(ipAddressNetwork));
        address = IpAddressCanonicalizer.Canonicalize(address);

        if (parts.Length == 1)
        {
            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? address + "/255.255.255.255"
                : address + "/128";
        }

        string net = parts[1];
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (int.TryParse(net, out int prefixLength))
            {
                if (prefixLength is < 0 or > 32)
                    throw new ArgumentException(nameof(NetworkInputError.InvalidSubnetMask), nameof(ipAddressNetwork));
                net = PrefixLengthToIpv4Mask(prefixLength);
            }
            else if (!IsValidSubnetMask(net))
            {
                throw new ArgumentException(nameof(NetworkInputError.InvalidSubnetMask), nameof(ipAddressNetwork));
            }
            else
            {
                net = IPAddress.Parse(net).ToString();
            }
        }
        else if (!int.TryParse(net, out int prefixLength) || prefixLength is < 0 or > 128)
        {
            throw new ArgumentException(nameof(NetworkInputError.InvalidIpv6PrefixLength), nameof(ipAddressNetwork));
        }

        return address + "/" + net;
    }

    private static string PrefixLengthToIpv4Mask(int prefixLength)
    {
        uint mask = prefixLength == 0 ? 0U : uint.MaxValue << (32 - prefixLength);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
            mask >> 24, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff);
    }

    private enum NetworkInputError
    {
                /// <summary>
        /// 定義 InvalidIpAddress 列舉值。
        /// </summary>
InvalidIpAddress,
                /// <summary>
        /// 定義 InvalidIpv6PrefixLength 列舉值。
        /// </summary>
InvalidIpv6PrefixLength,
                /// <summary>
        /// 定義 InvalidSubnetMask 列舉值。
        /// </summary>
InvalidSubnetMask
    }



    /// <summary>
    /// Sets smtp password.
    /// </summary>
    /// <param name="password">password參數。</param>
    public void SetSmtpPassword(string password) => SmtpPassword = CryptoHelper.Encrypt(password, true);


    //        public void WriteAgentConfiguration(IDDSCommunity.IntrusionDetection.Api.Plugin.IAgentConfiguration agentConfiguration) {
    //            // find the agent first
    //            if (!Database.Instance.IsConfigured) configureDatabase();
    //            Guid agentId = GetAgentId(agentConfiguration.AgentName);

    //            string writeConfigCmd;
    //            if (agentId != Guid.Empty) {
    //                writeConfigCmd = @"update SecurityAgents set HardLockAttempts = @p2, HardLockTimeHours = @p3,
    //LockForever = @p4, SoftLockAttempts = @p5, SoftLockTimeMinutes=@p6, OverwriteConfiguration=@p7 where AssemblyName = @p0";
    //            } else {
    //                // agent not configured in database
    //                agentId = Guid.NewGuid();
    //                writeConfigCmd = @"insert into SecurityAgents(AssemblyName, AgentId, HardLockAttempts, HardLockTimeHours,
    //LockForever, SoftLockAttempts, SoftLockTimeMinutes, OverwriteConfiguration) values (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)";
    //            }
    //            Database.Instance.ExecuteNonQuery(writeConfigCmd, agentConfiguration.AssemblyName, agentId.ToString(),
    //                agentConfiguration.HardLockAttempts, agentConfiguration.HardLockDurationHrs,
    //                agentConfiguration.NeverUnlock, agentConfiguration.SoftLockAttempts, agentConfiguration.SoftLockDurationMins, agentConfiguration.OverwriteConfiguration);

    //        }

    //public Guid GetAgentId(string displayName) {
    //    string cmd = "Select AgentId from  SecurityAgents where DisplayName like @p0";
    //    try {
    //        object result = Database.Instance.ExecuteScalar(cmd, displayName);
    //        Guid agentId;
    //        if (result != null && Guid.TryParse(result.ToString(), out agentId)) return agentId;
    //    } catch (Exception) {
    //        throw;
    //    }
    //    return Guid.Empty;
    //}
    /// <summary>
    /// Gets soft lock minutes.
    /// </summary>
    /// <param name="agent">agent參數。</param>
    /// <returns>傳回get soft lock minutes結果。</returns>
    public int GetSoftLockMinutes(SecurityAgent agent)
    {
        if (agent.OverrideConfig)
        {
            return agent.SoftLockTimeMinutes;
        }
        return SoftLockTimeMinutes;
    }

    /// <summary>
    /// Gets hard lock hours.
    /// </summary>
    /// <param name="agent">agent參數。</param>
    /// <returns>傳回get hard lock hours結果。</returns>
    public int GetHardLockHours(SecurityAgent agent)
    {
        int hardLockHours;
        if (agent.OverrideConfig)
        {
            hardLockHours = agent.HardLockTimeHours;
            if (agent.LockForever) hardLockHours = (int)DateTime.MaxValue.Subtract(DateTime.Now).TotalHours;
        }
        else
        {
            hardLockHours = HardLockTimeHours;
            if (LockForever) hardLockHours = (int)DateTime.MaxValue.Subtract(DateTime.Now).TotalHours;
        }
        return hardLockHours;
    }

    //         public const string TABLE_SECURITY_AGENTS = @"
    //CREATE TABLE SecurityAgents(
    //    AgentId uniqueidentifier PRIMARY KEY not null,
    //    HardLockAttempts int NOT NULL,
    //	HardLockTimeHours int NOT NULL,
    //	LockForever bit NOT NULL,
    //	SoftLockAttempts int NOT NULL,
    //	SoftLockTimeMinutes int NOT NULL,
    //    CurrentConfigSet int null
    //)";

    //        public const string TABLE_SECURITY_AGENT_CONFIG = @"
    //CREATE TABLE SecurityAgentConfig(
    //    ConfigSet int not null,
    //    AgentId uniqueidentifier not null,
    //    PropertyName uniqueidentifier not null,
    //    PropertyValueString nvarchar(255) null
    //)";


}

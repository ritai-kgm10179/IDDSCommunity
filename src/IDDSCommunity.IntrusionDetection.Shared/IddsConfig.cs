using System;
using System.Collections.Generic;
using System.Net;

using System.Data;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class IddsConfig
{
    public const int DefaultSoftLockAttempts = 10;
    public const int DefaultSoftLockMinutes = 1;
    public const int DefaultHardLockAttempts = 20;
    public const int DefaultHardLockHours = 1;
    /// <summary>
    /// 取得未設定郵件伺服器時顯示的標準 SMTP 連接埠。
    /// </summary>
    public const int DefaultSmtpPort = 25;
    private readonly Database database;
    private readonly HashSet<string> changedAppConfigKeys = [];

    public const int ENABLED_FEATURES_FREE = 1;
    public const int ENABLED_FEATURES_PRO = 2;
    public const int CONFIG_DB_VERSION_NUMBER = 1;
    public const string LICENSE_FILE = "idds.vl";

    public const string CONFIG_VALUE_IS_DEBUG = "Configuration.IsDebug";
    public const string CONFIG_VALUE_LANGUAGE = "Configuration.Language";
    public const string CONFIG_VALUE_FIREWALL_BLOCK_MODE = "Configuration.FirewallBlockMode";


    private const int IDDS_PRODUCT_ID = 0x66;
    // production server

    private static IddsConfig? _instance;
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
        if (!database.IsConfigured) configureDatabase();
        Dictionary<string, string> config = [];
        using IDataReader rdr = database.ExecuteReader(string.Format("select ConfigKey, ConfigValue from {0}", configTable));
        while (rdr.Read())
        {
            config.Add(Db.DbValueConverter.ToString(rdr["ConfigKey"]), Db.DbValueConverter.ToString(rdr["ConfigValue"]));
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
        try
        {
            string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!string.IsNullOrEmpty(commonAppData))
            {
                string targetDir = System.IO.Path.Combine(commonAppData, "IDDS Community");
                string legacyDir = System.IO.Path.Combine(commonAppData, "IDDSCommunity");

                if (!System.IO.Directory.Exists(targetDir))
                {
                    if (System.IO.Directory.Exists(legacyDir))
                    {
                        try
                        {
                            System.IO.Directory.Move(legacyDir, targetDir);
                        }
                        catch
                        {
                            System.IO.Directory.CreateDirectory(targetDir);
                        }
                    }
                    else
                    {
                        System.IO.Directory.CreateDirectory(targetDir);
                    }
                }
                else if (System.IO.Directory.Exists(legacyDir))
                {
                    try
                    {
                        foreach (string file in System.IO.Directory.EnumerateFiles(legacyDir, "*", System.IO.SearchOption.AllDirectories))
                        {
                            string relPath = System.IO.Path.GetRelativePath(legacyDir, file);
                            string destFile = System.IO.Path.Combine(targetDir, relPath);
                            string? destSubDir = System.IO.Path.GetDirectoryName(destFile);
                            if (!string.IsNullOrEmpty(destSubDir))
                                System.IO.Directory.CreateDirectory(destSubDir);
                            if (!System.IO.File.Exists(destFile))
                                System.IO.File.Move(file, destFile);
                        }
                    }
                    catch
                    {
                        // 忽略個別檔案移轉失敗
                    }
                }
                return targetDir;
            }
        }
        catch
        {
            // 測試隔離環境備援
        }
        return AppDomain.CurrentDomain.BaseDirectory;
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

    public string ApplicationPath { get; set; } = GetDefaultDataDirectory();

    public int ConfigVersionNumber { get; set; }
    public DateTime? Expires { get; set; }
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

    public int HardLockAttempts { get; set; }
    public int HardLockTimeHours { get; set; }
    public bool LockForever { get; set; }
    public int SoftLockAttempts { get; set; }
    public int SoftLockTimeMinutes { get; set; }
    public bool UseSafeNetworkList { get; set; }
    public static string PluginDirectory { get; set; } = string.Empty;

    /* private string _hardwareId;
    private string GetHardwareId() {
        if (String.IsNullOrEmpty(_hardwareId)) {

            _hardwareId = KeyHelper.GetCurrentHardwareId();
        }
        return _hardwareId;
    } */


    public bool CyberSheriffContributor { get; set; }

    private CSafeNetworks? _safeNetworks;
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

    public class CSafeNetworks : List<CSafeNetwork> { }
    public class CSafeNetwork
    {
        public string IpAddress { get; set; } = string.Empty;
        public string SubnetMask { get; set; } = string.Empty;
        public string DisplayName => string.Format("{0}/{1}", IpAddress, SubnetMask);
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

    public bool SendInfoMail { get; set; }

    public string NotificationEmailAddress { get; set; } = string.Empty;

    public string SmtpServer { get; set; } = string.Empty;

    public int SmtpPort { get; set; }

    public string SmtpUsername { get; set; } = string.Empty;

    public bool SmtpSslRequired { get; set; }

    private string _smtpPassword = string.Empty;

    public string SmtpPassword
    {
        get => _smtpPassword; set => _smtpPassword = value;
    }

    public string SenderEmailAddress { get; set; } = string.Empty;

    public bool SmtpRequiresAuthentication { get; set; }

    public bool WebBasedMonitoring { get; set; }

    private bool? _isDebug;
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
            var address = IPAddress.Parse(ipAddress);
            foreach (CSafeNetwork net in SafeNetworks)
            {
                try
                {
                    if (IPAddress.Parse(net.IpAddress).AddressFamily.Equals(address.AddressFamily))
                    {
                        switch (address.AddressFamily)
                        {
                            case System.Net.Sockets.AddressFamily.InterNetwork:
                                result = IsIp4InNetwork(address, IPAddress.Parse(net.IpAddress), net.SubnetMask);
                                break;
                            case System.Net.Sockets.AddressFamily.InterNetworkV6:
                                result = IsIp6InNetwork(address, IPAddress.Parse(net.IpAddress), int.Parse(net.SubnetMask));
                                break;
                        }
                    }
                    if (result) return true;
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

    private List<IPAddress>? _localAddresses;

    /// <summary>
    /// Determines whether ip address local.
    /// </summary>
    /// <param name="address">address參數。</param>
    /// <returns>若ip address local傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsIpAddressLocal(IPAddress address)
    {
        if (_localAddresses == null)
        {
            _localAddresses = [];
            foreach (System.Net.NetworkInformation.NetworkInterface iface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                System.Net.NetworkInformation.IPInterfaceProperties iprop = iface.GetIPProperties();
                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation info in iprop.UnicastAddresses)
                {
                    _localAddresses.Add(info.Address);
                }
            }
        }
        return _localAddresses.Contains(address);
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
        InvalidIpAddress,
        InvalidIpv6PrefixLength,
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表安全性代理程式在資料庫中持久化之組態設定與中繼資料實體。
/// </summary>
[Serializable]
public class SecurityAgent : IAgentFilter
{

        /// <summary>
    /// 當 StatisticsUpdated 時引發之事件。
    /// </summary>
public event EventHandler? StatisticsUpdated;
    /// <summary>
    /// 初始化 <see cref="SecurityAgent"/> class的新執行個體。
    /// </summary>
    public SecurityAgent() { }
    /// <summary>
    /// 初始化 <see cref="SecurityAgent"/> class的新執行個體。
    /// </summary>
    /// <param name="name">name參數。</param>
    /// <param name="id">id參數。</param>
    public SecurityAgent(string name, Guid id)
        : this(name) => Id = id;
    /// <summary>
    /// 初始化 <see cref="SecurityAgent"/> class的新執行個體。
    /// </summary>
    /// <param name="name">name參數。</param>
    public SecurityAgent(string name) => Name = name;
    /// <summary>
    /// 初始化 <see cref="SecurityAgent"/> class的新執行個體。
    /// </summary>
    /// <param name="name">name參數。</param>
    /// <param name="failedLogins">failed logins參數。</param>
    /// <param name="hardLocks">hard locks參數。</param>
    /// <param name="softLocks">soft locks參數。</param>
    /// <param name="icon">icon參數。</param>
    public SecurityAgent(string name, int failedLogins, int hardLocks, int softLocks, Image icon)
        : this(name)
    {
        FailedLogins = failedLogins;
        HardLocks = hardLocks;
        SoftLocks = softLocks;
        Icon = icon;
    }

    /// <summary>
    /// 初始化 <see cref="SecurityAgent"/> class的新執行個體。
    /// </summary>
    /// <param name="name">name參數。</param>
    /// <param name="id">id參數。</param>
    /// <param name="failedLogins">failed logins參數。</param>
    /// <param name="hardLocks">hard locks參數。</param>
    /// <param name="softLocks">soft locks參數。</param>
    /// <param name="icon">icon參數。</param>
    public SecurityAgent(string name, Guid id, int failedLogins, int hardLocks, int softLocks, Image icon)
        : this(name, failedLogins, hardLocks, softLocks, icon) => Id = id;
    /// <summary>
    /// 執行check config version by id作業。
    /// </summary>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CheckConfigVersionById()
    {
        if (Id.Equals(Guid.Empty)) return false;
        string sqlCommand = "Select Serial from SecurityAgents where AgentId=@p0";
        object? dbVersion = Database.Instance.ExecuteScalar(sqlCommand, Id.ToString());
        if (dbVersion != null)
        {
            if (Db.DbValueConverter.ToInt(dbVersion) > Serial)
            {
                Reload();
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// 執行check config version by name作業。
    /// </summary>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CheckConfigVersionByName()
    {
        string sqlCommand = "Select Serial from SecurityAgents where Name=@p0";
        object? dbVersion = Database.Instance.ExecuteScalar(sqlCommand, Name);
        if (dbVersion != null)
        {
            if (Db.DbValueConverter.ToInt(dbVersion) > Serial)
            {
                Reload();
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// 執行reload作業。
    /// </summary>
    public void Reload()
    {
        if (!Database.Instance.IsConfigured)
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
        }
        if (Id.Equals(Guid.Empty)) return;
        using IDataReader rdr = Database.Instance.ExecuteReader("select * from securityAgents where AgentId=@p0", Id.ToString());
        // load all agents
        if (rdr.Read())
        {
            Name = Db.DbValueConverter.ToString(rdr["Name"]);
            AssemblyName = Db.DbValueConverter.ToString(rdr["AssemblyName"]);
            Id = Db.DbValueConverter.ToGuid(rdr["AgentId"]);
            HardLockAttempts = Db.DbValueConverter.ToInt(rdr["HardLockAttempts"]);
            HardLockTimeHours = Db.DbValueConverter.ToInt(rdr["HardLockTimeHours"]);
            LockForever = Db.DbValueConverter.ToBool(rdr["LockForever"]);
            SoftLockAttempts = Db.DbValueConverter.ToInt(rdr["SoftLockAttempts"]);
            SoftLockTimeMinutes = Db.DbValueConverter.ToInt(rdr["SoftLockTimeMinutes"]);
            OverrideConfig = Db.DbValueConverter.ToBool(rdr["OverwriteConfiguration"]);
            DisplayName = Db.DbValueConverter.ToString(rdr["DisplayName"]);
            Enabled = Db.DbValueConverter.ToBool(rdr["Enabled"]);
            Serial = Db.DbValueConverter.ToInt(rdr["Serial"]);
        }
        LoadCustomConfig();
    }
    /// <summary>
    /// 載入自訂 Agent 設定。
    /// </summary>
    public void LoadCustomConfig()
    {
        if (CustomConfigurationTypes.Count > 0)
        {
            List<string> unsupportedKeys = [];
            foreach (string key in CustomConfiguration.Keys)
            {
                if (!CustomConfigurationTypes.ContainsKey(key))
                    unsupportedKeys.Add(key);
            }
            foreach (string key in unsupportedKeys)
                CustomConfiguration.Remove(key);
        }

        using IDataReader rdr = DatabaseInstance.ExecuteReader("select PropertyName,PropertyValueString from SecurityAgentConfig where AgentId like @p0", Id.ToString());
        while (rdr.Read())
        {
            string propName = Shared.Db.DbValueConverter.ToString(rdr["PropertyName"]);
            if (CustomConfigurationTypes.Count > 0 && !CustomConfigurationTypes.ContainsKey(propName))
                continue;
            string propVal = Shared.Db.DbValueConverter.ToString(rdr["PropertyValueString"]);
            CustomConfiguration[propName] = propVal;
        }
    }

        /// <summary>
    /// 取得或設定 Name。
    /// </summary>
public string Name { get; set; } = string.Empty;

        /// <summary>
    /// 取得或設定 FailedLogins。
    /// </summary>
public int FailedLogins { get; set; }
        /// <summary>
    /// 取得或設定 HardLocks。
    /// </summary>
public int HardLocks { get; set; }
        /// <summary>
    /// 取得或設定 SoftLocks。
    /// </summary>
public int SoftLocks { get; set; }
    private byte[] _selectedIcon = [];
        /// <summary>
    /// 取得或設定 SelectedIcon。
    /// </summary>
public Image SelectedIcon
    {
        get => FromByte(_selectedIcon); set => _selectedIcon = FromImage(value);
    }
    /// <summary>
    /// 執行from image作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回from image結果。</returns>
    private static byte[] FromImage(Image value)
    {
        using MemoryStream ms = new();
        value.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
    /// <summary>
    /// 執行from byte作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回from byte結果。</returns>
    private static Image FromByte(byte[] value)
    {
        if (value.Length == 0)
            return new Bitmap(Resources.agent15px_default_dark);

        using MemoryStream stream = new(value, writable: false);
        using Image decoded = Image.FromStream(stream);
        return new Bitmap(decoded);
    }


    private byte[] _unselectedIcon = [];
        /// <summary>
    /// 取得或設定 UnselectedIcon。
    /// </summary>
public Image UnselectedIcon
    {
        get => FromByte(_unselectedIcon); set => _unselectedIcon = FromImage(value);
    }

    private byte[] _icon = [];
        /// <summary>
    /// 取得或設定 Icon。
    /// </summary>
public Image Icon
    {
        get => FromByte(_icon); set => _icon = FromImage(value);
    }
    [System.Xml.Serialization.XmlIgnore]
    [NonSerialized]
    private Database? _database;

    /// <summary>
    /// 取得或設定目前 SecurityAgent 關聯的 SQLite 資料庫執行個體。
    /// </summary>
    public Database DatabaseInstance
    {
        get => _database ?? Database.Instance;
        set => _database = value;
    }

    /// <summary>
    /// 儲存設定變更作業。
    /// </summary>
    public void Save()
    {
        if (Id == Guid.Empty) Id = GetId();
        string agentIdStr = Id.ToString();
        DatabaseInstance.ExecuteInTransaction((_, trans) =>
        {
            string updateSql = @"UPDATE SecurityAgents SET
                AssemblyName = @p1, HardLockAttempts = @p2, HardLockTimeHours = @p3,
                LockForever = @p4, SoftLockAttempts = @p5, SoftLockTimeMinutes = @p6,
                OverwriteConfiguration = @p7, DisplayName = @p8, Enabled = @p9, Name = @p10
                WHERE AgentId = @p0";

            DatabaseInstance.ExecuteNonQuery(updateSql, trans, agentIdStr, AssemblyName, HardLockAttempts, HardLockTimeHours,
                LockForever, SoftLockAttempts, SoftLockTimeMinutes, OverrideConfig, DisplayName, Enabled, Name);

            object? checkExists = DatabaseInstance.ExecuteScalar("SELECT count(*) FROM SecurityAgents WHERE AgentId = @p0", trans, agentIdStr);
            if (Shared.Db.DbValueConverter.ToInt(checkExists) == 0)
            {
                string insertSql = @"INSERT INTO SecurityAgents(AgentId, AssemblyName, HardLockAttempts, HardLockTimeHours,
                    LockForever, SoftLockAttempts, SoftLockTimeMinutes, OverwriteConfiguration, DisplayName, Enabled, Name, Serial)
                    VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, 0)";
                DatabaseInstance.ExecuteNonQuery(insertSql, trans, agentIdStr, AssemblyName, HardLockAttempts, HardLockTimeHours,
                    LockForever, SoftLockAttempts, SoftLockTimeMinutes, OverrideConfig, DisplayName, Enabled, Name);
            }

            DatabaseInstance.ExecuteNonQuery("UPDATE SecurityAgents SET Serial = Serial + 1 WHERE AgentId = @p0", trans, agentIdStr);
            Serial++;
            SaveCustomConfig(trans);
        });
        OnStatisticsUpdated();
    }

    /// <summary>
    /// 執行does exist in db作業。
    /// </summary>
    /// <param name="id">id參數。</param>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool DoesExistInDb(Guid id)
    {
        string sqlString = "select AgentId from SecurityAgents where AgentId = @p0";
        object? result = Database.Instance.ExecuteScalar(sqlString, id.ToString());
        if (result != null && Guid.TryParse(result.ToString(), out Guid agentId) && id.Equals(agentId)) return true;
        return false;
    }
    /// <summary>
    /// 使用呼叫者擁有的交易判斷 Agent 是否存在。
    /// </summary>
    /// <param name="transaction">擁有資料庫連線的交易物件。</param>
    /// <param name="id">Agent 識別碼。</param>
    /// <returns><see langword="true"/> when the Agent exists; otherwise, <see langword="false"/>.</returns>
    private static bool DoesExistInDb(IDbTransaction transaction, Guid id)
    {
        object? result = Database.Instance.ExecuteScalar("select AgentId from SecurityAgents where AgentId = @p0", transaction, id.ToString());
        return result is not null && Guid.TryParse(result.ToString(), out Guid agentId) && id.Equals(agentId);
    }
    /// <summary>
    /// 儲存自訂 Agent 設定。
    /// </summary>
    public void SaveCustomConfig() => DatabaseInstance.ExecuteInTransaction((_, transaction) => SaveCustomConfig(transaction));
    /// <summary>
    /// 使用呼叫者擁有的交易持久化自訂 Agent 設定。
    /// </summary>
    /// <param name="transaction">擁有資料庫連線的交易物件。</param>
    private void SaveCustomConfig(IDbTransaction transaction)
    {
        string agentIdStr = Id.ToString();
        if (CustomConfigurationTypes.Count > 0)
        {
            IEnumerable<string> storedKeys = DatabaseInstance.Query<string>(
                "select PropertyName from SecurityAgentConfig where AgentId = @AgentId",
                new { AgentId = agentIdStr }, transaction);
            foreach (string storedKey in storedKeys)
            {
                if (!CustomConfigurationTypes.ContainsKey(storedKey))
                {
                    DatabaseInstance.ExecuteNonQuery(
                        "delete from SecurityAgentConfig where AgentId = @p0 and PropertyName = @p1",
                        transaction, agentIdStr, storedKey);
                }
            }
        }
        foreach (string key in CustomConfiguration.Keys)
        {
            object? dbResult = DatabaseInstance.ExecuteScalar("select count(*) from SecurityAgentConfig where AgentId like @p0 and PropertyName like @p1", transaction, agentIdStr, key);
            int found = Shared.Db.DbValueConverter.ToInt(dbResult);
            string sql;
            if (found > 0)
            {
                sql = "update SecurityAgentConfig set PropertyValueString = @p0 where AgentId like @p1 and PropertyName like @p2";
            }
            else
            {
                sql = "insert into SecurityAgentConfig (PropertyValueString, AgentId, PropertyName) values(@p0,@p1,@p2)";
            }
            DatabaseInstance.ExecuteNonQuery(sql, transaction, CustomConfiguration[key], agentIdStr, key);
        }
    }
    /// <summary>
    /// 更新統計資料。
    /// </summary>
    public void UpdateStatistics()
    {
        string sqlString = "select FailedLogins, HardLocks, SoftLocks from AgentStatistics where AgentId=@p0";
        int hardLocks, failedLogins, softLocks;
        try
        {
            IDataReader rdr = Database.Instance.ExecuteReader(sqlString, Id);
            if (rdr.Read())
            {
                hardLocks = Db.DbValueConverter.ToInt(rdr["HardLocks"]);
                failedLogins = Db.DbValueConverter.ToInt(rdr["FailedLogins"]);
                softLocks = Db.DbValueConverter.ToInt(rdr["SoftLocks"]);
                if (hardLocks != HardLocks || softLocks != SoftLocks || failedLogins != FailedLogins)
                {
                    HardLocks = hardLocks;
                    SoftLocks = softLocks;
                    FailedLogins = failedLogins;
                    OnStatisticsUpdated();
                }
            }
            else
            {
                HardLocks = 0;
                FailedLogins = 0;
                SoftLocks = 0;
            }
            rdr.Close();

        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Security-agent statistics refresh failed: {0}", exception);
        }
    }
    /// <summary>
    /// 處理統計資料更新通知。
    /// </summary>
    private void OnStatisticsUpdated() => StatisticsUpdated?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 取得識別碼 (ID)。
    /// </summary>
    /// <returns>傳回get id結果。</returns>
    public Guid GetId()
    {
        if (!Id.Equals(Guid.Empty)) return Id;
        // if agent does not provide ID, set the ID from this agent. Otherwise read from database
        if (!DatabaseInstance.IsConfigured) DatabaseInstance.Configure(IddsConfig.GetDefaultDataDirectory());
        object? result = DatabaseInstance.ExecuteScalar("Select AgentId from SecurityAgents where Name = @p0", Name);
        if (result != null)
        {
            var id = Db.DbValueConverter.ToGuid(result);
            if (id != Guid.Empty)
            {
                return id;
            }
        }
        if (!string.IsNullOrEmpty(Name))
        {
            return new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(Name)));
        }
        return Guid.NewGuid();
    }

        /// <summary>
    /// 取得或設定 Id。
    /// </summary>
public Guid Id { get; set; }
        /// <summary>
    /// 取得或設定 硬封鎖失敗次數門檻。
    /// </summary>
public int HardLockAttempts { get; set; }
        /// <summary>
    /// 取得或設定 軟封鎖失敗次數門檻。
    /// </summary>
public int SoftLockAttempts { get; set; }
        /// <summary>
    /// 取得或設定 軟封鎖持續分鐘數。
    /// </summary>
public int SoftLockTimeMinutes { get; set; }
        /// <summary>
    /// 取得或設定 硬封鎖持續時數。
    /// </summary>
public int HardLockTimeHours { get; set; }
        /// <summary>
    /// 取得或設定 OverrideConfig。
    /// </summary>
public bool OverrideConfig { get; set; }
        /// <summary>
    /// 取得或設定 本地化顯示名稱。
    /// </summary>
public string DisplayName { get; set; } = string.Empty;
        /// <summary>
    /// 取得或設定 是否永久封鎖。
    /// </summary>
public bool LockForever { get; set; }
        /// <summary>
    /// 取得或設定 是否已啟用。
    /// </summary>
public bool Enabled { get; set; }
        /// <summary>
    /// 取得或設定 Serial。
    /// </summary>
public int Serial { get; set; }
        /// <summary>
    /// 取得或設定 AssemblyName。
    /// </summary>
public string AssemblyName { get; set; } = string.Empty;
        /// <summary>
    /// 取得或設定 AssemblyFilename。
    /// </summary>
public string AssemblyFilename { get; set; } = string.Empty;
        /// <summary>
    /// 取得或設定 BinaryMissing。
    /// </summary>
public bool BinaryMissing { get; set; }
        /// <summary>
    /// 取得或設定 AppDomain。
    /// </summary>
public AppDomain AppDomain { get; set; } = AppDomain.CurrentDomain;
    /// <summary>
    /// 取得目前鎖定型別。
    /// </summary>
    /// <param name="IpAddress">ip address參數。</param>
    /// <returns>傳回get current lock type結果。</returns>
    public LockType GetCurrentLockType(string IpAddress)
    {
        int unsuccessfulLogins = IntrusionLog.GetIncidentsByAgentId(Id, IpAddress);
        if (OverrideConfig)
        {
            if (unsuccessfulLogins >= HardLockAttempts) return LockType.HardLockRequested;
            if (unsuccessfulLogins >= SoftLockAttempts) return LockType.SoftLockRequested;
            return LockType.None;
        }
        else
        {
            if (unsuccessfulLogins >= IddsConfig.Instance.HardLockAttempts) return LockType.HardLockRequested;
            if (unsuccessfulLogins >= IddsConfig.Instance.SoftLockAttempts) return LockType.SoftLockRequested;
            return LockType.None;
        }
    }

    private Dictionary<string, string>? _customConfiguration;
        /// <summary>
    /// 取得或設定 CustomConfiguration。
    /// </summary>
public Dictionary<string, string> CustomConfiguration
    {
        get
        {
            _customConfiguration ??= [];
            return _customConfiguration;
        }

        set => _customConfiguration = value;
    }

    /// <summary>
    /// 取得或設定 Agent 自訂設定的原廠預設值快照。
    /// </summary>
    public Dictionary<string, string> DefaultCustomConfiguration { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 將此 Agent 的可調整設定恢復為原廠預設值，同時保留啟用狀態與識別資訊。
    /// </summary>
    public void ResetConfigurationToDefaults()
    {
        HardLockAttempts = IddsConfig.DefaultHardLockAttempts;
        HardLockTimeHours = IddsConfig.DefaultHardLockHours;
        SoftLockAttempts = IddsConfig.DefaultSoftLockAttempts;
        SoftLockTimeMinutes = IddsConfig.DefaultSoftLockMinutes;
        LockForever = false;
        OverrideConfig = false;
        CustomConfiguration = new Dictionary<string, string>(DefaultCustomConfiguration, StringComparer.Ordinal);
    }

    private Dictionary<string, string>? _customConfigurationTypes;
        /// <summary>
    /// 取得或設定 CustomConfigurationTypes。
    /// </summary>
public Dictionary<string, string> CustomConfigurationTypes
    {
        get
        {
            _customConfigurationTypes ??= [];
            return _customConfigurationTypes;
        }

        set => _customConfigurationTypes = value;
    }

    /// <summary>
    /// 取得代理程式的分組排序權重。
    /// 系統與遠端存取：10
    /// Web 與網域服務：20
    /// 資料庫服務：30
    /// 郵件與傳輸服務：40
    /// 其他未分類擴充元件：90
    /// </summary>
    public int SortOrder => GetSortOrder(Name, DisplayName);

    /// <summary>
    /// 計算指定的代理程式名稱或顯示名稱的分組排序權重。
    /// </summary>
    /// <param name="name">代理程式識別名稱。</param>
    /// <param name="displayName">代理程式顯示名稱。</param>
    /// <returns>排序權重整數。</returns>
    public static int GetSortOrder(string? name, string? displayName)
    {
        string canonicalName = name ?? string.Empty;
        int namespaceSeparator = canonicalName.LastIndexOf('.');
        if (namespaceSeparator >= 0) canonicalName = canonicalName[(namespaceSeparator + 1)..];
        int canonicalOrder = canonicalName.ToUpperInvariant() switch
        {
            "WINDOWSNETWORKLOGONSECURITYAGENT" => 10,
            "TLSSSLAGENT" => 10,
            "RDGATEWAYSECURITYAGENT" => 10,
            "WINRMSECURITYAGENT" => 10,
            "OPENSSHSECURITYAGENT" => 10,
            "RADIUSSECURITYAGENT" => 10,
            "WEBSECURITYAGENT" => 20,
            "IISAUTHENTICATIONSECURITYAGENT" => 20,
            "WINDOWSDNSSECURITYAGENT" => 20,
            "TECHNITIUMDNSSECURITYAGENT" => 20,
            "SQLFAILEDLOGINWATCHER" => 30,
            "MYSQLFAILEDLOGINWATCHER" => 30,
            "POSTGRESQLSECURITYAGENT" => 30,
            "FILEMAKERSECURITYAGENT" => 30,
            "SMTPAGENT" => 40,
            "POP3AGENT" => 40,
            "IMAPAGENT" => 40,
            "FTPAGENT" => 40,
            "FILEZILLASECURITYAGENT" => 40,
            _ => 0
        };
        if (canonicalOrder != 0) return canonicalOrder;

        string identifier = (name ?? string.Empty) + " " + (displayName ?? string.Empty);

        // 1. 系統與遠端存取 (System & Remote Access)
        if (identifier.Contains("Windows Base", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("Windows Network Logon", StringComparison.OrdinalIgnoreCase) || identifier.Contains("網路登入", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("TLS/SSL", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("Remote Desktop Gateway", StringComparison.OrdinalIgnoreCase) || identifier.Contains("遠端桌面閘道", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("WinRM", StringComparison.OrdinalIgnoreCase) || identifier.Contains("Windows Remote Management", StringComparison.OrdinalIgnoreCase) || identifier.Contains("Windows 遠端管理", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("OpenSSH", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("AD Credential", StringComparison.OrdinalIgnoreCase) || identifier.Contains("AD 認證", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("Kerberos", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("RRAS", StringComparison.OrdinalIgnoreCase)) return 10;
        if (identifier.Contains("RADIUS", StringComparison.OrdinalIgnoreCase) || identifier.Contains("NPS", StringComparison.OrdinalIgnoreCase)) return 10;

        // 2. Web 與網域服務 (Web & Domain Services)
        if (identifier.Contains("Web Security", StringComparison.OrdinalIgnoreCase) || identifier.Contains("Web 安全", StringComparison.OrdinalIgnoreCase)) return 20;
        if (identifier.Contains("IIS Authentication", StringComparison.OrdinalIgnoreCase) || identifier.Contains("IIS 驗證", StringComparison.OrdinalIgnoreCase)) return 20;
        if (identifier.Contains("Windows DNS", StringComparison.OrdinalIgnoreCase)) return 20;
        if (identifier.Contains("Technitium", StringComparison.OrdinalIgnoreCase)) return 20;
        if (identifier.Contains("DNS", StringComparison.OrdinalIgnoreCase)) return 20;

        // 3. 資料庫服務 (Database Services)
        if (identifier.Contains("SQL Server", StringComparison.OrdinalIgnoreCase)) return 30;
        if (identifier.Contains("MySQL", StringComparison.OrdinalIgnoreCase) || identifier.Contains("MariaDB", StringComparison.OrdinalIgnoreCase)) return 30;
        if (identifier.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) || identifier.Contains("Postgres", StringComparison.OrdinalIgnoreCase)) return 30;
        if (identifier.Contains("FileMaker", StringComparison.OrdinalIgnoreCase)) return 30;

        // 4. 郵件與傳輸服務 (Mail & Network Protocols)
        if (identifier.Contains("SmtpAgent", StringComparison.OrdinalIgnoreCase) || identifier.Contains("SMTP", StringComparison.OrdinalIgnoreCase)) return 40;
        if (identifier.Contains("Pop3Agent", StringComparison.OrdinalIgnoreCase) || identifier.Contains("POP3", StringComparison.OrdinalIgnoreCase)) return 40;
        if (identifier.Contains("ImapAgent", StringComparison.OrdinalIgnoreCase) || identifier.Contains("IMAP", StringComparison.OrdinalIgnoreCase)) return 40;
        if (identifier.Contains("FTP", StringComparison.OrdinalIgnoreCase)) return 40;
        if (identifier.Contains("FileZilla", StringComparison.OrdinalIgnoreCase)) return 40;

        return 90;
    }
}


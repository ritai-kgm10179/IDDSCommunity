using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;

namespace Cyberarms.IntrusionDetection.Shared;

[Serializable]
public class SecurityAgent : IAgentFilter
{

    public event EventHandler? StatisticsUpdated;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgent"/> class.
    /// </summary>

    public SecurityAgent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgent"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="id">The id value.</param>

    public SecurityAgent(string name, Guid id)
        : this(name) => Id = id;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgent"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>

    public SecurityAgent(string name) => Name = name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgent"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="failedLogins">The failed logins value.</param>
    /// <param name="hardLocks">The hard locks value.</param>
    /// <param name="softLocks">The soft locks value.</param>
    /// <param name="icon">The icon value.</param>

    public SecurityAgent(string name, int failedLogins, int hardLocks, int softLocks, Image icon)
        : this(name)
    {
        FailedLogins = failedLogins;
        HardLocks = hardLocks;
        SoftLocks = softLocks;
        Icon = icon;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgent"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="id">The id value.</param>
    /// <param name="failedLogins">The failed logins value.</param>
    /// <param name="hardLocks">The hard locks value.</param>
    /// <param name="softLocks">The soft locks value.</param>
    /// <param name="icon">The icon value.</param>

    public SecurityAgent(string name, Guid id, int failedLogins, int hardLocks, int softLocks, Image icon)
        : this(name, failedLogins, hardLocks, softLocks, icon) => Id = id;

    /// <summary>
    /// Executes the check config version by id operation.
    /// </summary>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

    public bool CheckConfigVersionById()
    {
        if (Id.Equals(Guid.Empty)) return false;
        string sqlCommand = "Select Serial from SecurityAgents where AgentId=@p0";
        object? dbVersion = Database.Instance.ExecuteScalar(sqlCommand, Id);
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
    /// Executes the check config version by name operation.
    /// </summary>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

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
    /// Executes the reload operation.
    /// </summary>

    public void Reload()
    {
        if (!Database.Instance.IsConfigured)
        {
            throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
        }
        if (Id.Equals(Guid.Empty)) return;
        IDataReader rdr = Database.Instance.ExecuteReader("select * from securityAgents where AgentId=@p0", Id);
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
        rdr.Close();
        LoadCustomConfig();
    }

    /// <summary>
    /// Loads custom config.
    /// </summary>

    public void LoadCustomConfig()
    {
        IDataReader rdr = Database.Instance.ExecuteReader("select PropertyName,PropertyValueString from SecurityAgentConfig where AgentId like @p0", Id);
        while (rdr.Read())
        {
            string propName = Db.DbValueConverter.ToString(rdr["PropertyName"]);
            if (CustomConfiguration.ContainsKey(propName))
            {
                CustomConfiguration[propName] = Db.DbValueConverter.ToString(rdr["PropertyValueString"]);
            }
        }
        rdr.Close();
    }

    public string Name { get; set; } = string.Empty;

    public int FailedLogins { get; set; }
    public int HardLocks { get; set; }
    public int SoftLocks { get; set; }
    private byte[] _selectedIcon = [];
    public Image SelectedIcon
    {
        get => FromByte(_selectedIcon); set => _selectedIcon = FromImage(value);
    }

    /// <summary>
    /// Executes the from image operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The from image result.</returns>

    private static byte[] FromImage(Image value)
    {
        MemoryStream ms = new();
        value.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Executes the from byte operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The from byte result.</returns>

    private static Image FromByte(byte[] value)
    {
        if (value.Length == 0) return Resources.agent15px_default_dark;
        MemoryStream ms = new(value);
        return new Bitmap(ms);
    }


    private byte[] _unselectedIcon = [];
    public Image UnselectedIcon
    {
        get => FromByte(_unselectedIcon); set => _unselectedIcon = FromImage(value);
    }

    private byte[] _icon = [];
    public Image Icon
    {
        get => FromByte(_icon); set => _icon = FromImage(value);
    }

    /// <summary>
    /// Saves requested operation.
    /// </summary>

    public void Save()
    {
        if (Id == Guid.Empty) Id = GetId();
        string sqlString;
        IDbTransaction trans = Database.Instance.Connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            if (!DoesExistInDb(Id))
            {

                sqlString = @"INSERT INTO SecurityAgents(AgentId, AssemblyName,HardLockAttempts,HardLockTimeHours,
LockForever,SoftLockAttempts,SoftLockTimeMinutes,OverwriteConfiguration,DisplayName, Enabled, Name, Serial) 
values (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,0)";
            }
            else
            {
                sqlString = @"UPDATE SecurityAgents set AssemblyName = @p1,
HardLockAttempts=@p2, HardLockTimeHours=@p3, LockForever=@p4, SoftLockAttempts=@p5, SoftLockTimeMinutes=@p6,
OverwriteConfiguration=@p7, DisplayName=@p8, Enabled=@p9, Name=@p10 where AgentId=@p0";
            }
            Database.Instance.ExecuteNonQuery(sqlString, trans, Id, AssemblyName, HardLockAttempts, HardLockTimeHours,
                LockForever, SoftLockAttempts, SoftLockTimeMinutes, OverrideConfig, DisplayName, Enabled, Name);
            Database.Instance.ExecuteNonQuery("UPDATE SecurityAgents set Serial = Serial+1 where AgentId=@p0", trans, Id);
            SaveCustomConfig();
            trans.Commit();
            OnStatisticsUpdated();
        }
        catch (Exception)
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes the does exist in db operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

    public static bool DoesExistInDb(Guid id)
    {
        string sqlString = "select AgentId from SecurityAgents where AgentId = @p0";
        object? result = Database.Instance.ExecuteScalar(sqlString, id);
        if (result != null && Guid.TryParse(result.ToString(), out Guid agentId) && id.Equals(agentId)) return true;
        return false;
    }

    /// <summary>
    /// Saves custom config.
    /// </summary>

    public void SaveCustomConfig()
    {
        foreach (string key in CustomConfiguration.Keys)
        {
            //select PropertyName,PropertyValueString from SecurityAgentConfig where AgentId like @p0", Id);
            object? dbResult = Database.Instance.ExecuteScalar("select count(*) from SecurityAgentConfig where AgentId like @p0 and PropertyName like @p1", Id, key);
            int found = Db.DbValueConverter.ToInt(dbResult);
            string sql;
            if (found > 0)
            {
                sql = "update SecurityAgentConfig set PropertyValueString = @p0 where AgentId like @p1 and PropertyName like @p2";
            }
            else
            {
                sql = "insert into SecurityAgentConfig (PropertyValueString, AgentId, PropertyName) values(@p0,@p1,@p2)";
            }
            Database.Instance.ExecuteNonQuery(sql, CustomConfiguration[key], Id, key);
        }
    }

    /// <summary>
    /// Updates statistics.
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
        catch { }
    }

    /// <summary>
    /// Processes the statistics updated notification.
    /// </summary>

    private void OnStatisticsUpdated() => StatisticsUpdated?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Gets id.
    /// </summary>
    /// <returns>The get id result.</returns>

    public Guid GetId()
    {
        if (!Id.Equals(Guid.Empty)) return Id;
        // if agent does not provide ID, set the ID from this agent. Otherwise read from database
        if (!Database.Instance.IsConfigured) Database.Instance.Configure(IddsConfig.PluginDirectory);
        object? result = Database.Instance.ExecuteScalar("Select AgentId from SecurityAgents where AssemblyName = @p0", AssemblyName);
        if (result != null)
        {
            var id = Db.DbValueConverter.ToGuid(result);
            if (id != Guid.Empty)
            {
                return id;
            }
        }
        // last thing, return new guid --> should never happen when agents are configured properly
        return Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public int HardLockAttempts { get; set; }
    public int SoftLockAttempts { get; set; }
    public int SoftLockTimeMinutes { get; set; }
    public int HardLockTimeHours { get; set; }
    public bool OverrideConfig { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool LockForever { get; set; }
    public bool Enabled { get; set; }
    public int Serial { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public string AssemblyFilename { get; set; } = string.Empty;
    public bool BinaryMissing { get; set; }
    public AppDomain AppDomain { get; set; } = AppDomain.CurrentDomain;

    /// <summary>
    /// Gets current lock type.
    /// </summary>
    /// <param name="IpAddress">The ip address value.</param>
    /// <returns>The get current lock type result.</returns>

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
    public Dictionary<string, string> CustomConfiguration
    {
        get
        {
            _customConfiguration ??= [];
            return _customConfiguration;
        }

        set => _customConfiguration = value;
    }

}


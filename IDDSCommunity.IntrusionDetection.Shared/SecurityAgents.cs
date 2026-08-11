using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

[Serializable]
public class SecurityAgents : List<SecurityAgent>
{
    private readonly Database database;
    private readonly IddsConfig configuration;
    /// <summary>
    /// 初始化 <see cref="SecurityAgents"/> class的新執行個體。
    /// </summary>
    private SecurityAgents() : this(Database.Instance, IddsConfig.Instance)
    {
    }
    /// <summary>
    /// 初始化包含明確持久化與設定相依性的 Agent 集合。
    /// </summary>
    /// <param name="database">Agent 設定資料庫。</param>
    /// <param name="configuration">應用程式與擴充元件設定。</param>
    public SecurityAgents(Database database, IddsConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(configuration);
        this.database = database;
        this.configuration = configuration;
    }


    private static SecurityAgents? _instance;
    public static SecurityAgents Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SecurityAgents();
                _instance.InitializeAgents();
            }
            return _instance;
        }
    }
    /// <summary>
    /// 執行initialize agents作業。
    /// </summary>
    public void InitializeAgents()
    {
        Clear();
        if (!database.IsConfigured)
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
        }

        IDataReader rdr = database.ExecuteReader("select * from securityAgents");
        // load all agents
        while (rdr.Read())
        {
            SecurityAgent agent = new()
            {
                Name = Db.DbValueConverter.ToString(rdr["Name"]),
                AssemblyName = Db.DbValueConverter.ToString(rdr["AssemblyName"]),
                Id = Db.DbValueConverter.ToGuid(rdr["AgentId"]),
                HardLockAttempts = Db.DbValueConverter.ToInt(rdr["HardLockAttempts"]),
                HardLockTimeHours = Db.DbValueConverter.ToInt(rdr["HardLockTimeHours"]),
                LockForever = Db.DbValueConverter.ToBool(rdr["LockForever"]),
                SoftLockAttempts = Db.DbValueConverter.ToInt(rdr["SoftLockAttempts"]),
                SoftLockTimeMinutes = Db.DbValueConverter.ToInt(rdr["SoftLockTimeMinutes"]),
                OverrideConfig = Db.DbValueConverter.ToBool(rdr["OverwriteConfiguration"]),
                DisplayName = Db.DbValueConverter.ToString(rdr["DisplayName"]),
                Enabled = Db.DbValueConverter.ToBool(rdr["Enabled"]),
                Serial = Db.DbValueConverter.ToInt(rdr["Serial"])
            };
            agent.DatabaseInstance = database;
            agent.LoadCustomConfig();
            Add(agent);
        }
        rdr.Close();
    }
    /// <summary>
    /// 自磁碟讀取 Agent 設定。
    /// </summary>
    /// <returns>傳回read agents from disk結果。</returns>
    public List<SecurityAgent> ReadAgentsFromDisk()
    {
        if (string.IsNullOrEmpty(configuration.PluginsDirectory)) throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Application is not initialized."));
        List<SecurityAgent> result = [];
#if NETFRAMEWORK
        AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
        System.Security.Policy.Evidence adevidence = AppDomain.CurrentDomain.Evidence;
        CurrentDomain = AppDomain.CreateDomain("IDDSCommunity.Agents.Enumerator", adevidence, setup);
#else
        CurrentDomain = AppDomain.CurrentDomain;
#endif

        if (!Directory.Exists(configuration.PluginsDirectory))
        {
            Directory.CreateDirectory(configuration.PluginsDirectory);
        }

        foreach (string fileName in Directory.EnumerateFiles(configuration.PluginsDirectory, "IDDSCommunity.Agents.*.dll"))
        {
            Type tProxy = typeof(AgentLoaderProxy);
            string assemblyName = tProxy.Assembly.FullName ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Agent loader assembly has no full name."));
            string typeName = tProxy.FullName ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Agent loader type has no full name."));
            var proxy = (AgentLoaderProxy?)CurrentDomain.CreateInstanceAndUnwrap(assemblyName, typeName)
                ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create agent loader proxy."));
            List<SecurityAgent> agents = proxy.GetSecurityAgents(fileName, configuration.PluginsDirectory);
            foreach (SecurityAgent discoveredAgent in agents)
            {
                if (result.Exists(existingAgent => existingAgent.Id == discoveredAgent.Id))
                    throw new InvalidOperationException(Localization.Strings.Get("Agent plugin identifiers must be unique."));
            }
            result.AddRange(agents);
        }
        return result;
    }

    public AppDomain CurrentDomain { get; set; } = AppDomain.CurrentDomain;
    /// <summary>
    /// 依顯示名稱尋找 Agent。
    /// </summary>
    /// <param name="displayName">display name參數。</param>
    /// <returns>傳回find by display name結果。</returns>
    public SecurityAgent? FindByDisplayName(string displayName)
    {
        foreach (SecurityAgent agent in this)
        {
            if (agent.DisplayName == displayName)
            {
                return agent;
            }
        }
        return null;
    }
    /// <summary>
    /// 依名稱尋找 Agent。
    /// </summary>
    /// <param name="name">name參數。</param>
    /// <returns>傳回find by name結果。</returns>
    public SecurityAgent? FindByName(string name)
    {
        foreach (SecurityAgent agent in this)
        {
            if (agent.Name == name)
            {
                return agent;
            }
        }
        return null;
    }
    /// <summary>
    /// 取得顯示名稱。
    /// </summary>
    /// <param name="agentId">agent id參數。</param>
    /// <returns>傳回get display name結果。</returns>
    public string GetDisplayName(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId) || Guid.Empty.ToString().Equals(agentId, StringComparison.OrdinalIgnoreCase))
        {
            return Localization.Strings.Get("None");
        }

        bool isGuid = Guid.TryParse(agentId, out Guid targetGuid);

        foreach (SecurityAgent agent in this)
        {
            if (isGuid && agent.Id == targetGuid) return agent.DisplayName;
            if (string.Equals(agent.Id.ToString(), agentId, StringComparison.OrdinalIgnoreCase)) return agent.DisplayName;
            if (!string.IsNullOrEmpty(agent.Name) && string.Equals(agent.Name, agentId, StringComparison.OrdinalIgnoreCase)) return agent.DisplayName;
            if (!string.IsNullOrEmpty(agent.DisplayName) && string.Equals(agent.DisplayName, agentId, StringComparison.OrdinalIgnoreCase)) return agent.DisplayName;
        }

        try
        {
            if (database != null && database.IsConfigured)
            {
                object? dbDisplayName = database.ExecuteScalar("SELECT DisplayName FROM SecurityAgents WHERE AgentId = @p0 OR Name = @p0", agentId);
                if (dbDisplayName != null && !string.IsNullOrWhiteSpace(dbDisplayName.ToString()))
                {
                    return dbDisplayName.ToString()!;
                }
            }
        }
        catch (Exception exception)
        {
            const string component = "SecurityAgents-DisplayNameLookup";
            const string summary = "Unable to resolve an Agent display name from the configuration database.";
            System.Diagnostics.Trace.TraceWarning("{0}: {1}", summary, exception);
            _ = RollingDiagnosticLog.Write(component, summary, exception);
        }

        return Localization.Strings.Format("Agent {0} is not registered.", agentId);
    }
    /// <summary>
    /// 執行register security agents作業。
    /// </summary>
    public void RegisterSecurityAgents() => MergeDbInformation(ReadAgentsFromDisk());

    public Dictionary<SecurityAgent, AgentProxy> LoadedAgents { get; set; } = [];
    /// <summary>
    /// 執行unload agents作業。
    /// </summary>
    public void UnloadAgents()
    {
        if (LoadedAgents == null) return;
#if NETFRAMEWORK
        AppDomainManager adm = new AppDomainManager();
#endif

        foreach (AgentProxy proxy in LoadedAgents.Values)
        {
            proxy.Dispose();
#if NETFRAMEWORK
            AppDomain.Unload(proxy.AppDomain);
#endif
        }
        LoadedAgents.Clear();
    }
    /// <summary>
    /// 執行unload agent作業。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    public void UnloadAgent(SecurityAgent agent)
    {
#if NETFRAMEWORK
        AppDomain.Unload(agent.AppDomain);
#endif
        if (LoadedAgents.ContainsKey(agent))
        {
            LoadedAgents[agent].Dispose();
            LoadedAgents.Remove(agent);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Agent ") + agent.DisplayName + " is not loaded.");
        }
    }
    /// <summary>
    /// 處理應用程式域卸載事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void AppDomain_DomainUnload(object sender, EventArgs e) => System.Diagnostics.Debug.Print("Agent AppDomain unloaded");
    /// <summary>
    /// 載入 Agent 擴充元件。
    /// </summary>
    public void LoadAgents()
    {
        LoadedAgents ??= [];
        if (LoadedAgents.Count > 0) UnloadAgents();
        foreach (SecurityAgent agent in this)
        {
            if (agent.Enabled)
            {
                try
                {
#if NETFRAMEWORK
                    AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                    System.Security.Policy.Evidence adevidence = AppDomain.CurrentDomain.Evidence;
                    AppDomain domain = AppDomain.CreateDomain("IDDSCommunity.Agents." + agent.Id, adevidence, setup);
#else
                    AppDomain domain = AppDomain.CurrentDomain;
#endif
                    AgentProxy proxy = new(configuration.PluginsDirectory, agent.AssemblyFilename, agent.Name);
                    proxy.Configuration.AgentName = agent.Name;
                    proxy.Configuration.AssemblyName = agent.AssemblyName;
                    proxy.Configuration.Enabled = agent.Enabled;
                    proxy.Configuration.HardLockAttempts = agent.HardLockAttempts;
                    proxy.Configuration.HardLockDurationHrs = agent.HardLockTimeHours;
                    proxy.Configuration.NeverUnlock = agent.LockForever;
                    proxy.Configuration.OverwriteConfiguration = agent.OverrideConfig;
                    proxy.Configuration.SoftLockAttempts = agent.SoftLockAttempts;
                    proxy.Configuration.SoftLockDurationMins = agent.SoftLockTimeMinutes;
                    PluginConfiguration? pc = proxy.Configuration.AgentSettings;
                    if (pc != null)
                        ApplyCustomConfiguration(pc, agent.CustomConfiguration);
                    agent.AppDomain = domain;
                    agent.Reload();
                    LoadedAgents.Add(agent, proxy);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }

    internal static void ApplyCustomConfiguration(PluginConfiguration configuration, IReadOnlyDictionary<string, string> values)
    {
        foreach (PropertyInfo property in configuration.GetType().GetProperties())
        {
            if (!values.TryGetValue(property.Name, out string? value)) continue;
            if (property.PropertyType == typeof(string))
                property.SetValue(configuration, value, null);
            else if (property.PropertyType == typeof(int) && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int integerValue))
                property.SetValue(configuration, integerValue, null);
            else if (property.PropertyType == typeof(bool))
                property.SetValue(configuration, Db.DbValueConverter.ToBool(value), null);
        }
    }
    /// <summary>
    /// 啟動所有 Agent 擴充元件。
    /// </summary>
    public void StartAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            agent.Start();
        }
    }
    /// <summary>
    /// 停止所有 Agent 擴充元件。
    /// </summary>
    public void StopAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            agent.Stop();
        }
    }
    /// <summary>
    /// 執行pause agents作業。
    /// </summary>
    public void PauseAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            if (agent.CanPause())
            {
                agent.Pause();
            }
            else
            {
                agent.Stop();
            }
        }
    }
    /// <summary>
    /// 執行continue agents作業。
    /// </summary>
    public void ContinueAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            if (agent.CanContinue())
            {
                agent.Continue();
            }
            else
            {
                agent.Start();
            }
        }
    }

    /// <summary>
    /// 合併資料庫設定資訊至硬碟已載入之 Agent 清單。
    /// </summary>
    /// <param name="agents">硬碟所掃描出的 Agent 清單。</param>
    /// <returns>傳回合併後的 Agent 集合。</returns>
    public List<SecurityAgent> MergeDbInformation(List<SecurityAgent> agents)
    {
        List<SecurityAgent> result = [.. agents];
        foreach (SecurityAgent agent in this)
        {
            SecurityAgent? a = result.Find(x => x.Id == agent.Id);
            a ??= result.Find(x => !string.IsNullOrEmpty(agent.Name) && x.Name.Equals(agent.Name, StringComparison.OrdinalIgnoreCase));
            a ??= result.Find(x => !string.IsNullOrEmpty(agent.DisplayName) && x.DisplayName.Equals(agent.DisplayName, StringComparison.OrdinalIgnoreCase));
            // 僅以 Agent 型別短名稱處理舊版命名遷移；同一組件可能包含多個 Agent，禁止以組件名稱配對。
            if (a == null)
            {
                string dbShortName = GetShortName(agent.Name);
                a = result.Find(x =>
                    !string.IsNullOrEmpty(dbShortName)
                    && dbShortName.Equals(GetShortName(x.Name), StringComparison.OrdinalIgnoreCase));
            }

            if (a != null)
            {
                if (agent.Id == Guid.Empty) agent.Id = a.Id;
                agent.Name = a.Name;
                agent.AssemblyFilename = a.AssemblyFilename;
                agent.Icon = a.Icon;
                agent.SelectedIcon = a.SelectedIcon;
                agent.UnselectedIcon = a.UnselectedIcon;
                agent.DisplayName = a.DisplayName;
                agent.BinaryMissing = false;
                agent.CustomConfiguration = a.CustomConfiguration;
                agent.CustomConfigurationTypes = a.CustomConfigurationTypes;
                agent.LoadCustomConfig();
                result.Remove(a);
            }
            else
            {
                agent.Icon = Resources.agent15px_custom_dark;
                agent.SelectedIcon = Resources.agent15px_custom_white;
                agent.UnselectedIcon = Resources.agent15px_custom_dark;
                agent.BinaryMissing = true;
            }
        }
        foreach (SecurityAgent agent in result)
        {
            agent.Enabled = false;
        }
        AddRange(result);
        return this;
    }

    private static string GetShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return string.Empty;
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fullName);
        int idx = nameWithoutExt.LastIndexOf('.');
        return idx >= 0 ? nameWithoutExt[(idx + 1)..] : nameWithoutExt;
    }

    /// <summary>
    /// 取得清單索引。
    /// </summary>
    /// <param name="list">list參數。</param>
    /// <param name="name">name參數。</param>
    /// <returns>傳回get list index結果。</returns>
    private static int GetListIndex(List<SecurityAgent> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Name.Equals(name)) return i;
        }
        return -1;
    }



}

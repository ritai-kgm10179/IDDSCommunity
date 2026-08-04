using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Shared;

[Serializable]
public class SecurityAgents : List<SecurityAgent>
{
    private readonly Database database;
    private readonly IddsConfig configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAgents"/> class.
    /// </summary>

    private SecurityAgents() : this(Database.Instance, IddsConfig.Instance)
    {
    }

    /// <summary>
    /// Initializes an agent collection with explicit persistence and configuration dependencies.
    /// </summary>
    /// <param name="database">The agent configuration database.</param>
    /// <param name="configuration">The application and plug-in configuration.</param>
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
    /// Executes the initialize agents operation.
    /// </summary>

    public void InitializeAgents()
    {
        Clear();
        if (!database.IsConfigured)
        {
            throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
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
            //agent.LoadCustomConfig();
            Add(agent);
        }
        rdr.Close();
    }

    /// <summary>
    /// Reads agents from disk.
    /// </summary>
    /// <returns>The read agents from disk result.</returns>

    public List<SecurityAgent> ReadAgentsFromDisk()
    {
        if (string.IsNullOrEmpty(configuration.PluginsDirectory)) throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Application is not initialized."));
        List<SecurityAgent> result = [];
#if NETFRAMEWORK
        AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
        System.Security.Policy.Evidence adevidence = AppDomain.CurrentDomain.Evidence;
        CurrentDomain = AppDomain.CreateDomain("Cyberarms.Agents.Enumerator", adevidence, setup);
#else
        CurrentDomain = AppDomain.CurrentDomain;
#endif

        if (!Directory.Exists(configuration.PluginsDirectory))
        {
            Directory.CreateDirectory(configuration.PluginsDirectory);
        }

        foreach (string fileName in Directory.EnumerateFiles(configuration.PluginsDirectory, "*.dll"))
        {
            if (!fileName.Contains(".Api.dll"))
            {
                Type tProxy = typeof(AgentLoaderProxy);
                string assemblyName = tProxy.Assembly.FullName ?? throw new InvalidOperationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Agent loader assembly has no full name."));
                string typeName = tProxy.FullName ?? throw new InvalidOperationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Agent loader type has no full name."));
                var proxy = (AgentLoaderProxy?)CurrentDomain.CreateInstanceAndUnwrap(assemblyName, typeName)
                    ?? throw new InvalidOperationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create agent loader proxy."));
                List<SecurityAgent> agents = proxy.GetSecurityAgents(fileName);
                result.AddRange(agents);

            }
        }
        return result;
    }

    public AppDomain CurrentDomain { get; set; } = AppDomain.CurrentDomain;

    /// <summary>
    /// Finds by display name.
    /// </summary>
    /// <param name="displayName">The display name value.</param>
    /// <returns>The find by display name result.</returns>

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
    /// Finds by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The find by name result.</returns>

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
    /// Gets display name.
    /// </summary>
    /// <param name="agentId">The agent id value.</param>
    /// <returns>The get display name result.</returns>

    public string GetDisplayName(string agentId)
    {
        if (Guid.Empty.ToString().Equals(agentId))
        {
            return "None";
        }
        foreach (SecurityAgent agent in this)
        {
            if (agent.Id.ToString().Equals(agentId))
            {
                return agent.DisplayName;
            }
        }
        return string.Format("Agent {0} is not registered.", agentId);
    }

    /// <summary>
    /// Executes the register security agents operation.
    /// </summary>

    public void RegisterSecurityAgents() => MergeDbInformation(ReadAgentsFromDisk());

    public Dictionary<SecurityAgent, AgentProxy> LoadedAgents { get; set; } = [];

    /// <summary>
    /// Executes the unload agents operation.
    /// </summary>

    public void UnloadAgents()
    {
        if (LoadedAgents == null) return;
#if NETFRAMEWORK
        AppDomainManager adm = new AppDomainManager();
#endif

        foreach (SecurityAgent agent in LoadedAgents.Keys)
        {
#if NETFRAMEWORK
            AppDomain.Unload(agent.AppDomain);
#endif
        }
        LoadedAgents.Clear();
    }

    /// <summary>
    /// Executes the unload agent operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void UnloadAgent(SecurityAgent agent)
    {
#if NETFRAMEWORK
        AppDomain.Unload(agent.AppDomain);
#endif
        if (LoadedAgents.ContainsKey(agent))
        {
            LoadedAgents.Remove(agent);
        }
        else
        {
            throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Agent ") + agent.DisplayName + " is not loaded.");
        }
    }

    /// <summary>
    /// Handles the domain unload event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void AppDomain_DomainUnload(object sender, EventArgs e) => System.Diagnostics.Debug.Print("Agent AppDomain unloaded");

    /// <summary>
    /// Loads agents.
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
                    AppDomain domain = AppDomain.CreateDomain("Cyberarms.Agents." + agent.Id, adevidence, setup);
#else
                    AppDomain domain = AppDomain.CurrentDomain;
#endif
                    AgentProxy proxy = new(agent.AssemblyFilename, agent.Name);
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
                    {
                        foreach (PropertyInfo pi in pc.GetType().GetProperties())
                        {
                            if (agent.CustomConfiguration.ContainsKey(pi.Name))
                            {
                                if (pi.PropertyType == typeof(int))
                                {
                                    int.TryParse(agent.CustomConfiguration[pi.Name], out int result);
                                    pi.SetValue(pc, result, null);
                                }
                            }
                        }
                    }
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

    /// <summary>
    /// Starts agents.
    /// </summary>

    public void StartAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            agent.Start();
        }
    }

    /// <summary>
    /// Stops agents.
    /// </summary>

    public void StopAgents()
    {
        foreach (AgentProxy agent in LoadedAgents.Values)
        {
            agent.Stop();
        }
    }

    /// <summary>
    /// Executes the pause agents operation.
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
    /// Executes the continue agents operation.
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
    /// Merges db information.
    /// </summary>
    /// <param name="agents">The agents value.</param>
    /// <returns>The merge db information result.</returns>

    public List<SecurityAgent> MergeDbInformation(List<SecurityAgent> agents)
    {
        List<SecurityAgent> result = [.. agents];
        foreach (SecurityAgent agent in this)
        {
            int listIndex = GetListIndex(result, agent.Name);
            SecurityAgent? a = result.Find(x => x.Id == agent.Id);
            // fallback if previous installation was made
            a ??= (result.Find(x => x.Name == agent.Name));

            if (a != null)
            {
                agent.AssemblyFilename = a.AssemblyFilename;
                agent.Icon = a.Icon;
                agent.SelectedIcon = a.SelectedIcon;
                agent.UnselectedIcon = a.UnselectedIcon;
                agent.DisplayName = a.DisplayName;
                agent.BinaryMissing = false;
                agent.CustomConfiguration = a.CustomConfiguration;
                agent.LoadCustomConfig();
                result.Remove(a);
            }
            else
            {
                agent.Icon = Resources.agent15px_custom_dark;
                agent.SelectedIcon = Resources.agent15px_custom_white;
                agent.UnselectedIcon = Resources.agent15px_custom_dark;
                agent.BinaryMissing = true;
                agent.Enabled = false;
                if (a is not null)
                    Remove(a);
            }
            //int listIndex = GetListIndex(result, agent.Name);
            //if (listIndex >= 0) {
            //    agent.AssemblyFilename = result[listIndex].AssemblyFilename;
            //    agent.Icon = result[listIndex].Icon;
            //    agent.SelectedIcon = result[listIndex].SelectedIcon;
            //    agent.UnselectedIcon = result[listIndex].UnselectedIcon;
            //    agent.DisplayName = result[listIndex].DisplayName;
            //    agent.BinaryMissing = false;
            //    agent.CustomConfiguration = result[listIndex].CustomConfiguration;
            //    agent.LoadCustomConfig();
            //    result.RemoveAt(listIndex);
            //} else {
            //    agent.Icon = global::Cyberarms.IntrusionDetection.Shared.Resources.agent15px_custom_dark;
            //    agent.SelectedIcon = global::Cyberarms.IntrusionDetection.Shared.Resources.agent15px_custom_white;
            //    agent.UnselectedIcon = global::Cyberarms.IntrusionDetection.Shared.Resources.agent15px_custom_dark;
            //    agent.BinaryMissing = true;
            //    agent.Enabled = false;
            //}
        }
        foreach (SecurityAgent agent in result)
        {
            agent.Enabled = false;
        }
        AddRange(result);
        return this;
    }

    /// <summary>
    /// Gets list index.
    /// </summary>
    /// <param name="list">The list value.</param>
    /// <param name="name">The name value.</param>
    /// <returns>The get list index result.</returns>

    private static int GetListIndex(List<SecurityAgent> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Name.Equals(name)) return i;
        }
        return -1;
    }



}

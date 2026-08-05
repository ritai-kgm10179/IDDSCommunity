using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class AgentConfigurations : List<AgentConfigurationBase>
{
    /// <summary>
    /// Adds requested operation.
    /// </summary>
    /// <param name="agentConfig">The agent config value.</param>

    public new void Add(AgentConfigurationBase agentConfig)
    {
        if (!IsConfigured(agentConfig.AssemblyName, agentConfig.AgentName))
        {
            base.Add(agentConfig);
        }
        else
        {
            if (agentConfig == null)
            {
                throw new ArgumentException(string.Format("The configuration is not initialized!"));
            }
            else
            {
                throw new ArgumentException(string.Format("The configuration for {0} already exists and cannot be added!",
                    agentConfig.AgentName));
            }
        }
    }

    /// <summary>
    /// Determines whether configured.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <returns><see langword="true"/> if configured; otherwise, <see langword="false"/>.</returns>

    public bool IsConfigured(string assemblyName, string agentName)
    {
        // return GetAgentConfig(assemblyName, agentName) != null;
        foreach (IAgentConfiguration config in this)
        {
            if (config.AssemblyName == assemblyName && config.AgentName == agentName) return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether agent enabled.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <returns><see langword="true"/> if agent enabled; otherwise, <see langword="false"/>.</returns>

    public bool IsAgentEnabled(string assemblyName, string agentName)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        if (config == null) return false;
        return config.Enabled;
    }

    /// <summary>
    /// Gets agent config.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <returns>The get agent config result.</returns>

    public IAgentConfiguration GetAgentConfig(string assemblyName, string agentName) => GetAgentConfig(assemblyName, agentName, null);

    /// <summary>
    /// Gets agent config.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <param name="configurationSettingsType">The configuration settings type value.</param>
    /// <returns>The get agent config result.</returns>

    public IAgentConfiguration GetAgentConfig(string assemblyName, string agentName, string? configurationSettingsType)
    {
        foreach (IAgentConfiguration config in this)
        {
            if (assemblyName.Equals(config.AssemblyName) && agentName.Equals(config.AgentName))
            {
                return config;
            }
        }

        return CreateAgentConfig(assemblyName, agentName, configurationSettingsType);
    }

    /// <summary>
    /// Creates agent config.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <param name="configurationSettingsTypeName">The configuration settings type name value.</param>
    /// <returns>The create agent config result.</returns>

    public AgentConfigurationBase CreateAgentConfig(string assemblyName, string agentName, string? configurationSettingsTypeName)
    {
        AgentConfigurationBase newConfig = new()
        {
            AgentName = agentName,
            AssemblyName = assemblyName,
            Enabled = false,
            ConfigurationSettingsTypeName = configurationSettingsTypeName ?? string.Empty
        };
        Add(newConfig);
        return newConfig;
    }

    /// <summary>
    /// Executes the enable agent operation.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>

    public void EnableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, true);

    /// <summary>
    /// Executes the disable agent operation.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>

    public void DisableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, false);

    /// <summary>
    /// Sets enabled.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="agentName">The agent name value.</param>
    /// <param name="enabled">The enabled value.</param>

    public void SetEnabled(string assemblyName, string agentName, bool enabled)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        config?.Enabled = enabled;
    }

    /// <summary>
    /// Loads plugins from directory.
    /// </summary>
    /// <param name="pluginDirectory">The plugin directory value.</param>

    public void LoadPluginsFromDirectory(string pluginDirectory)
    {
        if (!System.IO.Directory.Exists(pluginDirectory)) return;
        string trustedDirectory = System.IO.Path.GetFullPath(pluginDirectory);
        foreach (string candidate in System.IO.Directory.EnumerateFiles(trustedDirectory, "*.dll", System.IO.SearchOption.TopDirectoryOnly))
        {
            string assemblyName = System.IO.Path.GetFullPath(candidate);
            if (!assemblyName.StartsWith(trustedDirectory + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            System.Reflection.Assembly? assembly = null;
            try
            {
                assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(System.IO.Path.GetFullPath(assemblyName));
            }
            catch (Exception ex)
            {
                OnLoadPluginExceptionRaised(assemblyName, null, ex, PluginExceptionSource.Init);
            }
            if (assembly != null)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsPublic && !type.IsAbstract)
                    {

                        Type? typeInterface = type.GetInterface(typeof(IAgentPlugin).Name, false);
                        //Make sure the interface we want to use actually exists
                        if (typeInterface != null)
                        {
                            try
                            {
                                var objectInstance = Activator.CreateInstance(type) as IAgentPlugin;
                                if (objectInstance != null)
                                {
                                    GetAgentConfig(assemblyName, type.Name, objectInstance.Configuration.ConfigurationSettingsTypeName);

                                }
                            }
                            catch (Exception exception)
                            {
                                OnLoadPluginExceptionRaised(assemblyName, type.Name, exception, PluginExceptionSource.Load);
                            }

                        }
                    }
                }
            }
        }
    }

    public event LoadPlugInExceptionRaisedHandler? LoadPluginExceptionRaised;
    public delegate void LoadPlugInExceptionRaisedHandler(object sender, PluginExceptionArguments data);

    /// <summary>
    /// Processes the load plugin exception raised notification.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="moduleName">The module name value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="source">The source value.</param>

    protected internal void OnLoadPluginExceptionRaised(string assemblyName, string? moduleName, Exception exception, PluginExceptionSource source)
    {
        PluginExceptionArguments args = new()
        {
            AssemblyName = assemblyName,
            ModuleName = moduleName,
            Exception = exception,
            Source = source
        };
        LoadPluginExceptionRaised?.Invoke(this, args);
    }

    /// <summary>
    /// Gets assembly names.
    /// </summary>
    /// <returns>The get assembly names result.</returns>

    public List<string> GetAssemblyNames()
    {
        List<string> result = [];
        foreach (AgentConfigurationBase config in this)
        {
            if (!result.Contains(config.AssemblyName)) result.Add(config.AssemblyName);
        }
        return result;
    }

    /// <summary>
    /// Gets modules.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <returns>The get modules result.</returns>

    public List<string> GetModules(string assemblyName)
    {
        List<string> result = [];
        foreach (AgentConfigurationBase config in this)
        {
            if (config.Equals(assemblyName))
            {
                if (!result.Contains(config.AgentName)) result.Add(config.AgentName);
            }
        }
        return result;
    }

}


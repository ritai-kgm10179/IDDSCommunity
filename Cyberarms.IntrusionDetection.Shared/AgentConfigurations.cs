using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Shared;

public class AgentConfigurations : List<AgentConfigurationBase>
{
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

    public bool IsConfigured(string assemblyName, string agentName)
    {
        // return GetAgentConfig(assemblyName, agentName) != null;
        foreach (IAgentConfiguration config in this)
        {
            if (config.AssemblyName == assemblyName && config.AgentName == agentName) return true;
        }
        return false;
    }

    public bool IsAgentEnabled(string assemblyName, string agentName)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        if (config == null) return false;
        return config.Enabled;
    }

    public IAgentConfiguration GetAgentConfig(string assemblyName, string agentName) => GetAgentConfig(assemblyName, agentName, null);

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

    public void EnableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, true);

    public void DisableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, false);

    public void SetEnabled(string assemblyName, string agentName, bool enabled)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        config?.Enabled = enabled;
    }

    public void LoadPluginsFromDirectory(string pluginDirectory)
    {
        foreach (string assemblyName in System.IO.Directory.GetFiles(pluginDirectory))
        {
            System.Reflection.Assembly? assembly = null;
            try
            {
                assembly = System.Reflection.Assembly.LoadFile(assemblyName);
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

    public List<string> GetAssemblyNames()
    {
        List<string> result = [];
        foreach (AgentConfigurationBase config in this)
        {
            if (!result.Contains(config.AssemblyName)) result.Add(config.AssemblyName);
        }
        return result;
    }

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


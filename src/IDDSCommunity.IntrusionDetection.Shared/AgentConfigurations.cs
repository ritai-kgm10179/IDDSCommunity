using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表安全性代理程式組態設定物件之集合清單。
/// </summary>
public class AgentConfigurations : List<AgentConfigurationBase>
{
    /// <summary>
    /// Adds requested operation.
    /// </summary>
    /// <param name="agentConfig">agent config參數。</param>
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
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <returns>若configured傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
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
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <returns>若agent enabled傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsAgentEnabled(string assemblyName, string agentName)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        if (config == null) return false;
        return config.Enabled;
    }
    /// <summary>
    /// Gets agent config.
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <returns>傳回get agent config結果。</returns>
    public IAgentConfiguration GetAgentConfig(string assemblyName, string agentName) => GetAgentConfig(assemblyName, agentName, null);
    /// <summary>
    /// Gets agent config.
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <param name="configurationSettingsType">configuration settings type參數。</param>
    /// <returns>傳回get agent config結果。</returns>
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
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <param name="configurationSettingsTypeName">configuration settings type name參數。</param>
    /// <returns>傳回create agent config結果。</returns>
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
    /// 執行enable agent作業。
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    public void EnableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, true);
    /// <summary>
    /// 執行disable agent作業。
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    public void DisableAgent(string assemblyName, string agentName) => SetEnabled(assemblyName, agentName, false);
    /// <summary>
    /// Sets enabled.
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="agentName">agent name參數。</param>
    /// <param name="enabled">enabled參數。</param>
    public void SetEnabled(string assemblyName, string agentName, bool enabled)
    {
        IAgentConfiguration config = GetAgentConfig(assemblyName, agentName);
        config?.Enabled = enabled;
    }
    /// <summary>
    /// Loads plugins from directory.
    /// </summary>
    /// <param name="pluginDirectory">plugin directory參數。</param>
    public void LoadPluginsFromDirectory(string pluginDirectory)
    {
        if (!System.IO.Directory.Exists(pluginDirectory)) return;
        string trustedDirectory = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(pluginDirectory));
        foreach (string candidate in System.IO.Directory.EnumerateFiles(trustedDirectory, "*.dll", System.IO.SearchOption.TopDirectoryOnly))
        {
            string assemblyName = candidate;
            System.Reflection.Assembly? assembly = null;
            try
            {
                assemblyName = PluginPathValidator.Validate(trustedDirectory, candidate);
                assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyName);
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

    /// <summary>
    /// 當載入擴充元件發生例外狀況時引發之事件。
    /// </summary>
    public event LoadPlugInExceptionRaisedHandler? LoadPluginExceptionRaised;

    /// <summary>
    /// 代表載入擴充元件發生例外狀況時之事件處理常式委派。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="data">包含擴充元件例外狀況詳細資訊之引數。</param>
    public delegate void LoadPlugInExceptionRaisedHandler(object sender, PluginExceptionArguments data);
    /// <summary>
    /// Processes the load plugin exception raised notification.
    /// </summary>
    /// <param name="assemblyName">assembly name參數。</param>
    /// <param name="moduleName">module name參數。</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="source">source參數。</param>
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
    /// <returns>傳回get assembly names結果。</returns>
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
    /// <param name="assemblyName">assembly name參數。</param>
    /// <returns>傳回get modules結果。</returns>
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


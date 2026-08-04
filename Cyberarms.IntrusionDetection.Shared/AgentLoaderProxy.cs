using System;
using System.Collections.Generic;
using System.Reflection;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Shared;

[Serializable]
public class AgentLoaderProxy : MarshalByRefObject
{
    private readonly List<AgentPluginLoadContext> loadContexts = [];
    /// <summary>
    /// Gets security agents.
    /// </summary>
    /// <param name="fileName">The file name value.</param>
    /// <returns>The get security agents result.</returns>

    public List<SecurityAgent> GetSecurityAgents(string fileName)
    {
        string pluginPath = System.IO.Path.GetFullPath(fileName);
        AgentPluginLoadContext loadContext = new(pluginPath);
        var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
        loadContexts.Add(loadContext);
        List<SecurityAgent> result = [];
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsPublic && !type.IsAbstract)
            {

                Type? typeInterface = type.GetInterface(typeof(IAgentPlugin).FullName!, false);
                //Make sure the interface we want to use actually exists
                if (typeInterface != null)
                {
                    try
                    {
                        string typeName = type.FullName ?? throw new InvalidOperationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Agent type has no full name."));
                        object? instance = Activator.CreateInstance(type);
                        if (instance is IAgentPlugin agentPlugin)
                        {
                            SecurityAgent securityAgent = new()
                            {
                                AssemblyName = assembly.FullName ?? assembly.GetName().Name ?? string.Empty
                            };
                            if (agentPlugin is IExtendedInformation)
                            {
                                var exInfo = (IExtendedInformation)agentPlugin;
                                securityAgent.DisplayName = exInfo.DisplayName;
                                securityAgent.UnselectedIcon = exInfo.UnselectedIcon;
                                securityAgent.SelectedIcon = exInfo.SelectedIcon;
                                securityAgent.Icon = exInfo.Icon;
                                securityAgent.Id = exInfo.Id;
                            }
                            else
                            {
                                securityAgent.DisplayName = typeName;
                                securityAgent.UnselectedIcon = Resources.agent15px_default_dark;
                                securityAgent.SelectedIcon = Resources.agent15px_default_white;
                                securityAgent.Icon = Resources.agent15px_default_dark;
                            }
                            securityAgent.Name = typeName;
                            securityAgent.Enabled = false;
                            securityAgent.FailedLogins = 0;
                            securityAgent.HardLockAttempts = 10;
                            securityAgent.HardLocks = 0;
                            securityAgent.HardLockTimeHours = 24;
                            securityAgent.AssemblyFilename = fileName;
                            securityAgent.SoftLockAttempts = 3;
                            securityAgent.SoftLocks = 0;
                            securityAgent.SoftLockTimeMinutes = 20;
                            securityAgent.OverrideConfig = false;
                            if (agentPlugin.Configuration.AgentSettings != null)
                            {
                                securityAgent.CustomConfiguration = GetCustomConfigurationObjects(agentPlugin.Configuration.AgentSettings);
                            }
                            result.Add(securityAgent);
                        }
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                        throw;
                    }

                }
            }
        }
        return result;
    }


    /// <summary>
    /// Gets custom configuration objects.
    /// </summary>
    /// <param name="config">The config value.</param>
    /// <returns>The get custom configuration objects result.</returns>

    public static Dictionary<string, string> GetCustomConfigurationObjects(PluginConfiguration config)
    {
        Dictionary<string, string> result = [];
        foreach (PropertyInfo pi in config.GetType().GetProperties())
        {
            result.Add(pi.Name, pi.GetValue(config, null)?.ToString() ?? string.Empty);
        }
        return result;
    }


}

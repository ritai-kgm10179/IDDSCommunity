using System;
using System.Collections.Generic;
using System.Reflection;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Shared;

[Serializable]
public class AgentLoaderProxy : MarshalByRefObject
{
    public List<SecurityAgent> GetSecurityAgents(string fileName)
    {
        var assembly = Assembly.LoadFile(fileName);
        List<SecurityAgent> result = [];
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsPublic && !type.IsAbstract)
            {

                Type typeInterface = type.GetInterface(typeof(IAgentPlugin).ToString(), false);
                //Make sure the interface we want to use actually exists
                if (typeInterface != null)
                {
                    try
                    {
                        var agentPlugin = (IAgentPlugin)Activator.CreateInstanceFrom(fileName, type.FullName.ToString()).Unwrap();
                        if (agentPlugin != null)
                        {
                            SecurityAgent securityAgent = new()
                            {
                                AssemblyName = assembly.FullName
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
                                securityAgent.DisplayName = type.FullName;
                                securityAgent.UnselectedIcon = Resources.agent15px_default_dark;
                                securityAgent.SelectedIcon = Resources.agent15px_default_white;
                                securityAgent.Icon = Resources.agent15px_default_dark;
                            }
                            securityAgent.Name = type.FullName;
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
                        throw exception;
                    }

                }
            }
        }
        return result;
    }


    public static Dictionary<string, string> GetCustomConfigurationObjects(PluginConfiguration config)
    {
        Dictionary<string, string> result = [];
        foreach (PropertyInfo pi in config.GetType().GetProperties())
        {
            result.Add(pi.Name, pi.GetValue(config, null).ToString());
        }
        return result;
    }


}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

[Serializable]
public class AgentLoaderProxy : MarshalByRefObject
{
    /// <summary>
    /// Gets security agents.
    /// </summary>
    /// <param name="fileName">The file name value.</param>
    /// <param name="pluginRoot">The trusted plug-in directory.</param>
    /// <returns>The get security agents result.</returns>

    public List<SecurityAgent> GetSecurityAgents(string fileName, string pluginRoot)
    {
        string pluginPath = PluginPathValidator.Validate(pluginRoot, fileName);
        AgentPluginLoadContext loadContext = new(pluginPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
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
                            string typeName = type.FullName ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Agent type has no full name."));
                            object? instance = Activator.CreateInstance(type);
                            if (instance is IAgentPlugin agentPlugin)
                            {
                                SecurityAgent securityAgent = new()
                                {
                                    AssemblyName = assembly.FullName ?? assembly.GetName().Name ?? string.Empty
                                };
                                if (agentPlugin is IExtendedInformation exInfo)
                                {
                                    securityAgent.DisplayName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get(exInfo.DisplayName);
                                    securityAgent.UnselectedIcon = NormalizeIcon(exInfo.UnselectedIcon);
                                    securityAgent.SelectedIcon = NormalizeIcon(exInfo.SelectedIcon);
                                    securityAgent.Icon = NormalizeIcon(exInfo.Icon);
                                    securityAgent.Id = exInfo.Id;
                                }
                                else
                                {
                                    securityAgent.DisplayName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get(typeName);
                                    securityAgent.UnselectedIcon = NormalizeIcon(Resources.agent15px_default_dark);
                                    securityAgent.SelectedIcon = NormalizeIcon(Resources.agent15px_default_white);
                                    securityAgent.Icon = NormalizeIcon(Resources.agent15px_default_dark);
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
                                    securityAgent.CustomConfiguration = GetCustomConfigurationObjects(agentPlugin.Configuration.AgentSettings);
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
        finally
        {
            loadContext.Unload();
        }
    }

    /// <summary>
    /// Normalizes Plugin artwork to the administration UI's standard visual icon size.
    /// </summary>
    /// <param name="source">The Plugin-provided icon.</param>
    /// <returns>A 16-by-16 pixel icon suitable for consistent list presentation.</returns>
    private static Image NormalizeIcon(Image source)
    {
        const int iconSize = 16;
        Bitmap result = new(iconSize, iconSize);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, iconSize, iconSize));
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

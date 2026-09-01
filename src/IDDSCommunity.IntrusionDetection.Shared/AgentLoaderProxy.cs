using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供跨 AppDomain/處理程序載入與分析擴充元件組件資訊之遠端代理類別。
/// </summary>
[Serializable]
public class AgentLoaderProxy : MarshalByRefObject
{
    /// <summary>
    /// Gets security agents.
    /// </summary>
    /// <param name="fileName">file name參數。</param>
    /// <param name="pluginRoot">The trusted plug-in directory.</param>
    /// <returns>傳回get security agents結果。</returns>
    public List<SecurityAgent> GetSecurityAgents(string fileName, string pluginRoot)
    {
        string pluginPath = PluginPathValidator.Validate(pluginRoot, fileName);
        AgentPluginLoadContext loadContext = new(pluginPath);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
            List<SecurityAgent> result = [];
            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = System.Linq.Enumerable.Where(exception.Types, type => type is not null)!;
            }

            foreach (Type type in types)
            {
                try
                {
                    if (type.IsPublic && !type.IsAbstract)
                    {
                        Type? typeInterface = type.GetInterface(typeof(IAgentPlugin).FullName!, false);
                        //Make sure the interface we want to use actually exists
                        if (typeInterface != null)
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
                                    AgentThemeCategory category = AgentThemeIconFactory.DetectCategory(securityAgent.DisplayName + " " + typeName);
                                    securityAgent.UnselectedIcon = NormalizeIcon(exInfo.UnselectedIcon, category, false);
                                    securityAgent.SelectedIcon = NormalizeIcon(exInfo.SelectedIcon, category, true);
                                    securityAgent.Icon = NormalizeIcon(exInfo.Icon, category, false);
                                    securityAgent.Id = exInfo.Id;
                                }
                                else
                                {
                                    securityAgent.DisplayName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get(typeName);
                                    AgentThemeCategory category = AgentThemeIconFactory.DetectCategory(securityAgent.DisplayName + " " + typeName);
                                    securityAgent.UnselectedIcon = NormalizeIcon(null, category, false);
                                    securityAgent.SelectedIcon = NormalizeIcon(null, category, true);
                                    securityAgent.Icon = NormalizeIcon(null, category, false);
                                }
                                securityAgent.Name = typeName;
                                securityAgent.Enabled = false;
                                securityAgent.FailedLogins = 0;
                                securityAgent.HardLockAttempts = IddsConfig.DefaultHardLockAttempts;
                                securityAgent.HardLocks = 0;
                                securityAgent.HardLockTimeHours = IddsConfig.DefaultHardLockHours;
                                securityAgent.AssemblyFilename = fileName;
                                securityAgent.SoftLockAttempts = IddsConfig.DefaultSoftLockAttempts;
                                securityAgent.SoftLocks = 0;
                                securityAgent.SoftLockTimeMinutes = IddsConfig.DefaultSoftLockMinutes;
                                securityAgent.OverrideConfig = false;
                                if (agentPlugin.Configuration.AgentSettings != null)
                                {
                                    securityAgent.CustomConfiguration = GetCustomConfigurationObjects(agentPlugin.Configuration.AgentSettings);
                                    securityAgent.DefaultCustomConfiguration = new Dictionary<string, string>(securityAgent.CustomConfiguration, StringComparer.Ordinal);
                                    securityAgent.CustomConfigurationTypes = GetCustomConfigurationTypes(agentPlugin.Configuration.AgentSettings);
                                }
                                result.Add(securityAgent);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.WriteLine(exception.ToString());
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
    /// <param name="fallbackCategory">The theme category fallback.</param>
    /// <param name="selected">Whether the icon represents selected state.</param>
    /// <returns>適用於清單呈現之 16x16 像素圖示。</returns>
    private static Image NormalizeIcon(Image? source, AgentThemeCategory fallbackCategory, bool selected)
    {
        Image actual = source ?? AgentThemeIconFactory.Create(fallbackCategory, selected);
        const int iconSize = 16;
        Bitmap result = new(iconSize, iconSize);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(actual, new Rectangle(0, 0, iconSize, iconSize));
        return result;
    }

    /// <summary>
    /// Gets custom configuration objects.
    /// </summary>
    /// <param name="config">config參數。</param>
    /// <returns>傳回get custom configuration objects結果。</returns>
    public static Dictionary<string, string> GetCustomConfigurationObjects(PluginConfiguration config)
    {
        Dictionary<string, string> result = [];
        foreach (PropertyInfo pi in config.GetType().GetProperties())
        {
            result.Add(pi.Name, pi.GetValue(config, null)?.ToString() ?? string.Empty);
        }
        return result;
    }

        /// <summary>
    /// 執行 GetCustomConfigurationTypes 作業。
    /// </summary>
public static Dictionary<string, string> GetCustomConfigurationTypes(PluginConfiguration config)
    {
        Dictionary<string, string> result = [];
        foreach (PropertyInfo property in config.GetType().GetProperties())
            result[property.Name] = property.PropertyType.FullName ?? property.PropertyType.Name;
        return result;
    }


}

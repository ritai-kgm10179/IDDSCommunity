using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace Cyberarms.IntrusionDetection.Api.Plugin;

/// <summary>
/// This class can be used as base class for custom configuration.
/// Using this base class, Intrusion Detection automatically loads and saves configuration values needed by your plugin.
/// </summary>
public class AgentConfigurationBase : IAgentConfiguration {
    public string AssemblyName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public bool Enabled { get; set; }

    private PluginConfiguration? _agentSettings;

    [System.Xml.Serialization.XmlIgnore]
    public PluginConfiguration? AgentSettings {
        get {
            if (_agentSettings is null && !string.IsNullOrEmpty(ConfigurationSettingsTypeName)) {
                Type? configType = GetConfigurationType();
                if (configType is not null) {
                    object? o = Activator.CreateInstance(configType);
                    if (o is PluginConfiguration pluginConfig) {
                        _agentSettings = pluginConfig;
                    }
                }
            }
            return _agentSettings;
        }
        set => _agentSettings = value;
    }

    public string ConfigurationSettingsTypeName { get; set; } = string.Empty;

    public string? PluginConfigurationXml {
        get {
            if (AgentSettings is null) return null;
            XmlSerializer xs = new(AgentSettings.GetType());
            StringBuilder sb = new();
            using StringWriter sw = new(sb);
            xs.Serialize(sw, AgentSettings);
            return sb.ToString();
        }
        set {
            if (AgentSettings is not null && value is not null) {
                XmlSerializer xs = new(AgentSettings.GetType());
                using StringReader sr = new(value);
                AgentSettings = (PluginConfiguration)xs.Deserialize(sr)!;
            }
        }
    }

    public int HardLockDurationHrs { get; set; }
    public int HardLockAttempts { get; set; }
    public int SoftLockDurationMins { get; set; }
    public int SoftLockAttempts { get; set; }
    public bool OverwriteConfiguration { get; set; }
    public bool NeverUnlock { get; set; }
    public string FileName { get; set; } = string.Empty;

    public void CloneFrom(IAgentConfiguration source) {
        AssemblyName = source.AssemblyName;
        AgentName = source.AgentName;
        ConfigurationSettingsTypeName = source.ConfigurationSettingsTypeName;
        Enabled = source.Enabled;
        HardLockAttempts = source.HardLockAttempts;
        HardLockDurationHrs = source.HardLockDurationHrs;
        SoftLockAttempts = source.SoftLockAttempts;
        SoftLockDurationMins = source.SoftLockDurationMins;
        OverwriteConfiguration = source.OverwriteConfiguration;
        NeverUnlock = source.NeverUnlock;
        if (!string.IsNullOrEmpty(ConfigurationSettingsTypeName) && source.AgentSettings is not null) {
            Type? configType = GetConfigurationType();
            if (configType is not null) {
                AgentSettings = (PluginConfiguration)Activator.CreateInstance(configType)!;
                AgentSettings.CloneFrom(source.AgentSettings);
            }
        }
    }

    public Type? GetConfigurationType() {
        if (File.Exists(AssemblyName)) {
            Assembly assembly = Assembly.LoadFile(AssemblyName);
            if (assembly is not null) {
                foreach (Type type in assembly.GetTypes()) {
                    if (type.IsPublic && !type.IsAbstract && type.FullName == ConfigurationSettingsTypeName) {
                        return type;
                    }
                }
            }
        }
        return null;
    }
}

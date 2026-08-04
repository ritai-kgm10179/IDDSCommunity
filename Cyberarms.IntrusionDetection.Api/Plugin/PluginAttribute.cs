using System;

namespace Cyberarms.IntrusionDetection.Api.Plugin;

/// <summary>
/// Custom attribute for plugins to specify displayname and description.
/// TheIntrusion Detectionadministration software displays the values defined as class attribute
/// </summary>
/// <remarks>
/// This attribute is displayed in theIntrusion Detectionadministration software
/// </remarks>
/// <param name="displayName">Name to display in the administration software</param>
public class PluginAttribute(string displayName) : Attribute
{
    /// <summary>
    /// This attribute is displayed in theIntrusion Detectionadministration software
    /// </summary>
    /// <param name="displayName">Name to display in the administration software</param>
    /// <param name="description">Short description of the agent</param>
    /// <param name="version">Version number of the agent</param>
    public PluginAttribute(string displayName, string description, string version)
        : this(displayName, description) => Version = version;

    /// <summary>
    /// This attribute is displayed in theIntrusion Detectionadministration software
    /// </summary>
    /// <param name="displayName">Name to display in the administration software</param>
    /// <param name="description">Short description of the agent</param>
    public PluginAttribute(string displayName, string description)
        : this(displayName) => Description = description;

    /// <summary>
    /// Display name of your agent
    /// </summary>
    public string DisplayName { get; set; } = displayName;
    /// <summary>
    /// Add a short description about what your agent does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Version number of your agent
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

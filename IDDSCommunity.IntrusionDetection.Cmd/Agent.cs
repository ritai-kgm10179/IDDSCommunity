using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Cmd;

internal class Agent
{
    internal string AssemblyName { get; set; } = string.Empty;
    internal bool Running { get; set; }
    internal Exception? LastException { get; set; }
    internal IAgentPlugin? Assembly { get; set; }
    internal string Name { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Agent"/> class.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>

    internal Agent(string assemblyName) => AssemblyName = assemblyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Agent"/> class.
    /// </summary>

    internal Agent() { }
}

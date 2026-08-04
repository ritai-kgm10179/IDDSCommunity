using System;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection;

internal class Agent
{
    internal string AssemblyName { get; set; } = string.Empty;
    internal bool Running { get; set; }
    internal Exception? LastException { get; set; }
    internal IAgentPlugin? Assembly { get; set; }
    internal string Name { get; set; } = string.Empty;

    internal Agent(string assemblyName)
    {
        AssemblyName = assemblyName;
    }

    internal Agent() { }
}

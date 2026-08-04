using System;
using System.Collections.Generic;
using System.Reflection;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Cmd;

internal class Agents : List<Agent>
{
    internal void Load(string assemblyName)
    {
        Type pInterfaceType = typeof(IAgentPlugin);
        var assembly = Assembly.LoadFile(assemblyName);
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsPublic && !type.IsAbstract)
            {
                Type? typeInterface = type.GetInterface(pInterfaceType.ToString(), false);

                if (typeInterface is not null)
                {
                    if (Activator.CreateInstance(type) is IAgentPlugin objectInstance)
                    {
                        Agent orange = new(assembly.FullName ?? assemblyName)
                        {
                            Assembly = objectInstance,
                            Name = type.Name
                        };
                        Add(orange);
                    }
                }
            }
        }
    }

    internal static void LoadAll(string configFilename) { }
}

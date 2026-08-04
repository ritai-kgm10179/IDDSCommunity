using System;
using System.Reflection;
using System.Runtime.Loader;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Shared;

internal sealed class AgentPluginLoadContext(string pluginPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver resolver = new(pluginPath);

    /// <summary>
    /// Loads a managed dependency from the plugin deployment directory.
    /// </summary>
    /// <param name="assemblyName">The requested assembly identity.</param>
    /// <returns>The loaded assembly, or <see langword="null"/> to use the default context.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == typeof(IAgentPlugin).Assembly.GetName().Name)
            return null;
        string? path = resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    /// <summary>
    /// Loads an unmanaged dependency from the plugin deployment directory.
    /// </summary>
    /// <param name="unmanagedDllName">The requested native library name.</param>
    /// <returns>A native library handle, or zero when resolution is delegated.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}

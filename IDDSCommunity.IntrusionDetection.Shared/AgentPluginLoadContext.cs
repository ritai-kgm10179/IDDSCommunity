using System;
using System.Reflection;
using System.Runtime.Loader;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal sealed class AgentPluginLoadContext(string pluginPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver resolver = new(pluginPath);

    private static readonly System.Collections.Generic.HashSet<string> SharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        typeof(IAgentPlugin).Assembly.GetName().Name!,
        typeof(SecurityAgent).Assembly.GetName().Name!
    };

    /// <summary>
    /// Loads a managed dependency from the plugin deployment directory.
    /// </summary>
    /// <param name="assemblyName">The requested assembly identity.</param>
    /// <returns>已載入的組件；若使用預設快取內容則傳回 <see langword="null"/>。</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
            return null;
        string? path = resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    /// <summary>
    /// Loads an unmanaged dependency from the plugin deployment directory.
    /// </summary>
    /// <param name="unmanagedDllName">The requested native library name.</param>
    /// <returns>原生函式庫句柄（Handle）；若交由委派處理則為零。</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}

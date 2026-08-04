using System;
using System.IO;

namespace Cyberarms.IntrusionDetection.Shared;

internal static class PluginPathValidator
{
    /// <summary>
    /// Resolves and validates a managed plug-in path beneath its configured root.
    /// </summary>
    /// <param name="pluginRoot">The configured plug-in directory.</param>
    /// <param name="pluginPath">The candidate assembly path.</param>
    /// <returns>The normalized full assembly path.</returns>
    internal static string Validate(string pluginRoot, string pluginPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginPath);
        string root = Path.GetFullPath(pluginRoot);
        string candidate = Path.GetFullPath(pluginPath);
        string relative = Path.GetRelativePath(root, candidate);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException(Localization.Strings.Get("Agent plugin path must remain inside the configured plugin directory."));
        if (!string.Equals(Path.GetExtension(candidate), ".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Localization.Strings.Get("Agent plugin must be a DLL assembly."));
        if (!File.Exists(candidate))
            throw new FileNotFoundException(Localization.Strings.Get("Agent plugin assembly was not found."), candidate);

        DirectoryInfo? directory = new FileInfo(candidate).Directory;
        while (directory is not null && directory.FullName.Length >= root.Length)
        {
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException(Localization.Strings.Get("Agent plugin path cannot contain a reparse point."));
            if (string.Equals(directory.FullName.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                break;
            directory = directory.Parent;
        }
        if ((File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException(Localization.Strings.Get("Agent plugin path cannot contain a reparse point."));
        return candidate;
    }
}

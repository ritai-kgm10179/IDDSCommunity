using System;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal static class PluginPathValidator
{
    /// <summary>
    /// 於設定的根目錄下解析並驗證受控擴充元件路徑。
    /// </summary>
    /// <param name="pluginRoot">設定的擴充元件目錄。</param>
    /// <param name="pluginPath">候選組件路徑。</param>
    /// <returns>傳回標準化的完整組件路徑。</returns>
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

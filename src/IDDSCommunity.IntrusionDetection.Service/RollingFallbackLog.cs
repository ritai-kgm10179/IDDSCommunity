using System;
using System.Globalization;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Service;
/// <summary>
/// 提供當 Windows 事件記錄無法使用時的每日輪替備援記錄。
/// </summary>
internal static class RollingFallbackLog
{
    private const int RetentionDays = 14;
    private static readonly object SyncRoot = new();

    internal static void Write(Exception eventLogFailure, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "IDDS Community", "Logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"runtime-{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(path, string.Format(CultureInfo.InvariantCulture,
                    "{0:O}\t{1}\tEventLogFailure={2}{3}", DateTime.UtcNow, LogSanitizer.Sanitize(message), eventLogFailure, Environment.NewLine));
                DateTime boundary = DateTime.UtcNow.AddDays(-RetentionDays);
                foreach (string candidate in Directory.EnumerateFiles(directory, "runtime-*.log"))
                    if (File.GetLastWriteTimeUtc(candidate) < boundary) File.Delete(candidate);
            }
        }
        catch
        {
            // A final logging fallback must never terminate the protection service.
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供不依賴 Windows 事件記錄的每日輪替診斷記錄。
/// </summary>
public static class RollingDiagnosticLog
{
    private const int RetentionDays = 14;
    private const long MaximumFileBytes = 5 * 1024 * 1024;
    private static readonly object SyncRoot = new();

    /// <summary>
    /// 寫入包含完整例外狀況資訊的診斷記錄，並清除逾期檔案。
    /// </summary>
    /// <param name="component">產生記錄的元件名稱。</param>
    /// <param name="message">不含機密資料的事件摘要。</param>
    /// <param name="exception">要記錄的例外狀況。</param>
    /// <returns>成功寫入時傳回記錄檔完整路徑；無法寫入時傳回 <see langword="null"/>。</returns>
    public static string? Write(string component, string message, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);
        ArgumentNullException.ThrowIfNull(exception);
        try
        {
            lock (SyncRoot)
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "IDDS Community", "Logs");
                Directory.CreateDirectory(directory);
                string prefix = SanitizeComponent(component);
                string path = SelectPath(directory, prefix, DateTime.UtcNow);
                string entry = JsonSerializer.Serialize(new DiagnosticEntry(
                    DateTimeOffset.UtcNow,
                    component,
                    message ?? string.Empty,
                    exception.GetType().FullName ?? exception.GetType().Name,
                    exception.Message,
                    exception.ToString()));
                File.AppendAllText(path, entry + Environment.NewLine, new System.Text.UTF8Encoding(false));
                DeleteExpiredFiles(directory, prefix, DateTime.UtcNow.AddDays(-RetentionDays));
                return path;
            }
        }
        catch
        {
            // 最終診斷備援不得遮蔽原始失敗或終止應用程式。
            return null;
        }
    }

    private static string SelectPath(string directory, string prefix, DateTime utcNow)
    {
        string date = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        for (int sequence = 0; ; sequence++)
        {
            string suffix = sequence == 0 ? string.Empty : $"-{sequence:D2}";
            string candidate = Path.Combine(directory, $"{prefix}-{date}{suffix}.jsonl");
            if (!File.Exists(candidate) || new FileInfo(candidate).Length < MaximumFileBytes)
                return candidate;
        }
    }

    private static void DeleteExpiredFiles(string directory, string prefix, DateTime boundaryUtc)
    {
        foreach (string candidate in Directory.EnumerateFiles(directory, $"{prefix}-*.jsonl"))
        {
            if (File.GetLastWriteTimeUtc(candidate) < boundaryUtc)
                File.Delete(candidate);
        }
    }

    private static string SanitizeComponent(string component)
    {
        char[] value = component.ToCharArray();
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int index = 0; index < value.Length; index++)
        {
            if (Array.IndexOf(invalid, value[index]) >= 0 || char.IsWhiteSpace(value[index]))
                value[index] = '-';
        }
        return new string(value);
    }

    private sealed record DiagnosticEntry(
        DateTimeOffset TimestampUtc,
        string Component,
        string Summary,
        string ExceptionType,
        string ExceptionMessage,
        string ExceptionDetail);
}

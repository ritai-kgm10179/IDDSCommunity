using System;
using System.Collections.Concurrent;
using System.Linq;

namespace IDDSCommunity.IntrusionDetection.Shared.Security;

/// <summary>
/// 提供針對用戶端 IP 暴力密碼破解防禦之滑動視窗失敗次數速率限制器。
/// </summary>
public sealed class FailedAttemptsRateLimiter
{
    private sealed class AttemptRecord
    {
        public int FailureCount { get; set; }
        public DateTime FirstFailureUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
    }

    private readonly int maxFailedAttempts;
    private readonly TimeSpan windowDuration;
    private readonly TimeSpan lockDuration;
    private readonly ConcurrentDictionary<string, AttemptRecord> records = new(StringComparer.OrdinalIgnoreCase);
    private DateTime lastCleanupUtc = DateTime.UtcNow;
    private readonly object cleanupLock = new();

    /// <summary>
    /// 初始化 <see cref="FailedAttemptsRateLimiter"/> 類別之新執行個體。
    /// </summary>
    /// <param name="maxFailedAttempts">在視窗期間內允許之最大失敗次數（預設為 10 次）。</param>
    /// <param name="windowDuration">計算失敗次數之滑動視窗長度（預設為 15 分鐘）。</param>
    /// <param name="lockDuration">觸發上限時之鎖定持續時間（預設為 15 分鐘）。</param>
    public FailedAttemptsRateLimiter(
        int maxFailedAttempts = 10,
        TimeSpan? windowDuration = null,
        TimeSpan? lockDuration = null)
    {
        this.maxFailedAttempts = Math.Max(1, maxFailedAttempts);
        this.windowDuration = windowDuration ?? TimeSpan.FromMinutes(15);
        this.lockDuration = lockDuration ?? TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// 評估指定用戶端 IP 目前是否處於暫時阻絕或鎖定狀態。
    /// </summary>
    /// <param name="ipAddress">用戶端 IP 位址字串。</param>
    /// <param name="retryAfter">若處於阻絕狀態，傳回剩餘之鎖定時間長度。</param>
    /// <returns>若已被阻絕則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsBlocked(string? ipAddress, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(ipAddress)) return false;

        DateTime now = DateTime.UtcNow;
        if (records.TryGetValue(ipAddress, out AttemptRecord? record))
        {
            lock (record)
            {
                if (record.LockedUntilUtc.HasValue)
                {
                    if (record.LockedUntilUtc.Value > now)
                    {
                        retryAfter = record.LockedUntilUtc.Value - now;
                        return true;
                    }
                    record.LockedUntilUtc = null;
                    record.FailureCount = 0;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 記錄指定用戶端 IP 發生一次身分驗證失敗，並在超過門檻時自動實施暫時阻絕。
    /// </summary>
    /// <param name="ipAddress">用戶端 IP 位址字串。</param>
    /// <returns>該用戶端 IP 目前累計之失敗次數。</returns>
    public int RecordFailedAttempt(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return 0;

        DateTime now = DateTime.UtcNow;
        AttemptRecord record = records.GetOrAdd(ipAddress, _ => new AttemptRecord
        {
            FirstFailureUtc = now,
            FailureCount = 0
        });

        int currentCount;
        lock (record)
        {
            if (now - record.FirstFailureUtc > windowDuration)
            {
                record.FirstFailureUtc = now;
                record.FailureCount = 1;
                record.LockedUntilUtc = null;
            }
            else
            {
                record.FailureCount++;
                if (record.FailureCount >= maxFailedAttempts)
                {
                    record.LockedUntilUtc = now + lockDuration;
                }
            }
            currentCount = record.FailureCount;
        }

        CleanupExpiredRecordsIfNeeded(now);
        return currentCount;
    }

    /// <summary>
    /// 取得指定用戶端 IP 目前於有效視窗內之失敗次數。
    /// </summary>
    /// <param name="ipAddress">用戶端 IP 位址字串。</param>
    /// <returns>若存在則傳回有效失敗次數；否則傳回 0。</returns>
    public int GetFailureCount(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return 0;
        if (records.TryGetValue(ipAddress, out AttemptRecord? record))
        {
            lock (record)
            {
                if (DateTime.UtcNow - record.FirstFailureUtc <= windowDuration)
                {
                    return record.FailureCount;
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// 重設指定用戶端 IP 之失敗嘗試計數（例如通過身分驗證時呼叫）。
    /// </summary>
    /// <param name="ipAddress">用戶端 IP 位址字串。</param>
    public void Reset(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return;
        records.TryRemove(ipAddress, out _);
    }

    private void CleanupExpiredRecordsIfNeeded(DateTime now)
    {
        if (now - lastCleanupUtc < TimeSpan.FromMinutes(5) && records.Count < 5000)
            return;

        lock (cleanupLock)
        {
            if (now - lastCleanupUtc < TimeSpan.FromMinutes(5) && records.Count < 5000)
                return;

            lastCleanupUtc = now;
            var expiredKeys = records.Where(pair =>
            {
                AttemptRecord rec = pair.Value;
                lock (rec)
                {
                    if (rec.LockedUntilUtc.HasValue && rec.LockedUntilUtc.Value > now) return false;
                    return (now - rec.FirstFailureUtc) > windowDuration;
                }
            }).Select(pair => pair.Key).ToList();

            foreach (string key in expiredKeys)
            {
                records.TryRemove(key, out _);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dapper;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;


internal sealed class ThreatHubStore(Database? database = null)
{
    internal const int MaximumEntries = 100000;
    internal const int PageSize = 256;
    internal const int WriteBatchSize = 500;
    private readonly object gate = new();
    private readonly Dictionary<string, Entry> memory = new(StringComparer.Ordinal);
    private readonly List<ThreatHubJournalEntry> memoryJournal = [];
    private long sequence;
    private long journalSequence;
    private readonly string memoryGeneration = Guid.NewGuid().ToString("N");
    internal string Generation => database is null ? memoryGeneration : Convert.ToString(database.ExecuteScalar("SELECT Generation FROM ThreatHubIdentity WHERE Id=1"))!;

    /// <summary>
    /// 取得目前情資庫中尚未過期之活躍威脅情資總筆數。
    /// </summary>
    internal int ActiveThreatCount
    {
        get
        {
            long now = DateTime.UtcNow.Ticks;
            lock (gate)
            {
                if (database is null)
                    return memory.Values.Count(e => e.ExpiresTicks > now);
                object? countObj = database.ExecuteScalar("SELECT COUNT(*) FROM ThreatHubEntries WHERE ExpiresTicks > @p0", now);
                return countObj != null && int.TryParse(countObj.ToString(), out int c) ? c : 0;
            }
        }
    }

    private long lastExpiredCleanupTicks;
    private const long CleanupIntervalTicks = TimeSpan.TicksPerMinute * 10;

    internal bool Upsert(ThreatIntelligenceItem item)
    {
        string json = JsonSerializer.Serialize(item);
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                if (now - lastExpiredCleanupTicks > CleanupIntervalTicks)
                {
                    foreach (string key in memory.Where(p => p.Value.ExpiresTicks <= now).Select(p => p.Key).ToArray()) memory.Remove(key);
                    lastExpiredCleanupTicks = now;
                }
                if (memory.TryGetValue(item.SourceIp, out Entry? previous) && previous.Payload == json) return true;
                if (!memory.ContainsKey(item.SourceIp) && memory.Count >= MaximumEntries) return false;
                memory[item.SourceIp] = new Entry { Sequence = ++sequence, Payload = json, ExpiresTicks = item.ExpiresUtc.Ticks };
                memoryJournal.Add(new ThreatHubJournalEntry(++journalSequence, ThreatHubJournalEventType.Blocked, item.SourceIp, json, now));
                return true;
            }
            bool accepted = false;
            database.ExecuteInTransaction((connection, transaction) =>
            {
                if (now - lastExpiredCleanupTicks > CleanupIntervalTicks)
                {
                    connection.Execute("DELETE FROM ThreatHubEntries WHERE ExpiresTicks<=@now", new { now }, transaction);
                    lastExpiredCleanupTicks = now;
                }
                string? previous = connection.ExecuteScalar<string>("SELECT Payload FROM ThreatHubEntries WHERE SourceIp=@ip", new { ip = item.SourceIp }, transaction);
                if (previous == json) { accepted = true; return; }
                if (previous is null && connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ThreatHubEntries", transaction: transaction) >= MaximumEntries) return;
                connection.Execute("DELETE FROM ThreatHubEntries WHERE SourceIp=@ip", new { ip = item.SourceIp }, transaction);
                connection.Execute("INSERT INTO ThreatHubEntries(SourceIp,Payload,ExpiresTicks) VALUES(@ip,@json,@expires)", new { ip = item.SourceIp, json, expires = item.ExpiresUtc.Ticks }, transaction);
                connection.Execute("INSERT INTO ThreatHubJournal(EventType,SourceIp,Payload,CreatedTicks) VALUES(@type,@ip,@json,@ticks)", new { type = ThreatHubJournalEventType.Blocked, ip = item.SourceIp, json, ticks = now }, transaction);
                accepted = true;
            });
            return accepted;
        }
    }

    /// <summary>
    /// 在單一資料庫交易中批次新增或更新威脅情報，大幅降低磁碟 I/O。
    /// </summary>
    /// <param name="items">威脅項目清單。</param>
    /// <returns>成功寫入之情資筆數。</returns>
    internal int UpsertBatch(IEnumerable<ThreatIntelligenceItem> items)
    {
        if (items == null) return 0;
        long now = DateTime.UtcNow.Ticks;
        int count = 0;
        lock (gate)
        {
            if (database is null)
            {
                if (now - lastExpiredCleanupTicks > CleanupIntervalTicks)
                {
                    foreach (string key in memory.Where(p => p.Value.ExpiresTicks <= now).Select(p => p.Key).ToArray()) memory.Remove(key);
                    lastExpiredCleanupTicks = now;
                }
                foreach (var item in items)
                {
                    string json = JsonSerializer.Serialize(item);
                    if (memory.TryGetValue(item.SourceIp, out Entry? previous) && previous.Payload == json) continue;
                    if (!memory.ContainsKey(item.SourceIp) && memory.Count >= MaximumEntries) break;
                    memory[item.SourceIp] = new Entry { Sequence = ++sequence, Payload = json, ExpiresTicks = item.ExpiresUtc.Ticks };
                    memoryJournal.Add(new ThreatHubJournalEntry(++journalSequence, ThreatHubJournalEventType.Blocked, item.SourceIp, json, now));
                    count++;
                }
                return count;
            }

            foreach (ThreatIntelligenceItem[] chunk in items
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.SourceIp))
                .Chunk(WriteBatchSize))
            {
                int committedCount = 0;
                bool cleanupCommitted = false;
                database.ExecuteInTransaction((connection, transaction) =>
                {
                    int attemptCount = 0;
                    bool shouldCleanup = now - lastExpiredCleanupTicks > CleanupIntervalTicks;
                    if (shouldCleanup)
                    {
                        connection.Execute("DELETE FROM ThreatHubEntries WHERE ExpiresTicks<=@now", new { now }, transaction);
                    }
                    int remainingCapacity = Math.Max(0, MaximumEntries - connection.ExecuteScalar<int>("SELECT COUNT(*) FROM ThreatHubEntries", transaction: transaction));

                    using var readCmd = connection.CreateCommand();
                    readCmd.Transaction = transaction;
                    readCmd.CommandText = "SELECT Payload FROM ThreatHubEntries WHERE SourceIp=@ip;";
                    var pReadIp = readCmd.Parameters.Add("@ip", Microsoft.Data.Sqlite.SqliteType.Text);

                    using var deleteCmd = connection.CreateCommand();
                    deleteCmd.Transaction = transaction;
                    deleteCmd.CommandText = "DELETE FROM ThreatHubEntries WHERE SourceIp=@ip;";
                    var pDeleteIp = deleteCmd.Parameters.Add("@ip", Microsoft.Data.Sqlite.SqliteType.Text);

                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = "INSERT INTO ThreatHubEntries(SourceIp,Payload,ExpiresTicks) VALUES(@ip,@json,@expires);";
                    var pIp = insertCmd.Parameters.Add("@ip", Microsoft.Data.Sqlite.SqliteType.Text);
                    var pJson = insertCmd.Parameters.Add("@json", Microsoft.Data.Sqlite.SqliteType.Text);
                    var pExpires = insertCmd.Parameters.Add("@expires", Microsoft.Data.Sqlite.SqliteType.Integer);

                    using var insertJournalCmd = connection.CreateCommand();
                    insertJournalCmd.Transaction = transaction;
                    insertJournalCmd.CommandText = "INSERT INTO ThreatHubJournal(EventType,SourceIp,Payload,CreatedTicks) VALUES(@type,@ip,@json,@ticks);";
                    var pJournalType = insertJournalCmd.Parameters.Add("@type", Microsoft.Data.Sqlite.SqliteType.Integer);
                    var pJournalIp = insertJournalCmd.Parameters.Add("@ip", Microsoft.Data.Sqlite.SqliteType.Text);
                    var pJournalJson = insertJournalCmd.Parameters.Add("@json", Microsoft.Data.Sqlite.SqliteType.Text);
                    var pJournalTicks = insertJournalCmd.Parameters.Add("@ticks", Microsoft.Data.Sqlite.SqliteType.Integer);

                    foreach (ThreatIntelligenceItem item in chunk)
                    {
                        string json = JsonSerializer.Serialize(item);
                        pReadIp.Value = item.SourceIp;
                        string? previous = readCmd.ExecuteScalar() as string;
                        if (previous == json) continue;
                        if (previous is null && remainingCapacity == 0) continue;

                        if (previous is not null)
                        {
                            pDeleteIp.Value = item.SourceIp;
                            deleteCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            remainingCapacity--;
                        }

                        pIp.Value = item.SourceIp;
                        pJson.Value = json;
                        pExpires.Value = item.ExpiresUtc.Ticks;
                        insertCmd.ExecuteNonQuery();

                        pJournalType.Value = ThreatHubJournalEventType.Blocked;
                        pJournalIp.Value = item.SourceIp;
                        pJournalJson.Value = json;
                        pJournalTicks.Value = now;
                        insertJournalCmd.ExecuteNonQuery();

                        attemptCount++;
                    }

                    committedCount = attemptCount;
                    cleanupCommitted = shouldCleanup;
                });
                count += committedCount;
                if (cleanupCommitted) lastExpiredCleanupTicks = now;
            }
            return count;
        }
    }

    /// <summary>
    /// 撤銷指定之威脅情資，並記錄撤銷墓碑（Tombstone）日誌。
    /// </summary>
    /// <param name="sourceIp">欲撤銷之來源 IP 位址。</param>
    /// <param name="reason">撤銷原因或附帶說明。</param>
    /// <returns>若成功撤銷傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    internal bool Revoke(string sourceIp, string reason = "")
    {
        if (string.IsNullOrWhiteSpace(sourceIp)) return false;
        if (!IpAddressCanonicalizer.TryCanonicalize(sourceIp, out string canonicalIp)) return false;
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                bool removed = memory.Remove(canonicalIp);
                memoryJournal.Add(new ThreatHubJournalEntry(++journalSequence, ThreatHubJournalEventType.Revoked, canonicalIp, reason ?? string.Empty, now));
                return removed;
            }
            bool success = false;
            database.ExecuteInTransaction((connection, transaction) =>
            {
                int affected = connection.Execute("DELETE FROM ThreatHubEntries WHERE SourceIp=@ip", new { ip = canonicalIp }, transaction);
                connection.Execute("INSERT INTO ThreatHubJournal(EventType,SourceIp,Payload,CreatedTicks) VALUES(@type,@ip,@json,@ticks)",
                    new { type = ThreatHubJournalEventType.Revoked, ip = canonicalIp, json = reason ?? string.Empty, ticks = now }, transaction);
                success = affected > 0;
            });
            return success;
        }
    }

    /// <summary>
    /// 記錄假釋觀察事件至異動日誌。
    /// </summary>
    /// <param name="sourceIp">假釋觀察之來源 IP 位址。</param>
    /// <param name="payload">附帶情資載體或說明。</param>
    /// <returns>若成功記錄傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    internal bool RecordProbation(string sourceIp, string payload = "")
    {
        if (string.IsNullOrWhiteSpace(sourceIp)) return false;
        if (!IpAddressCanonicalizer.TryCanonicalize(sourceIp, out string canonicalIp)) return false;
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                memoryJournal.Add(new ThreatHubJournalEntry(++journalSequence, ThreatHubJournalEventType.Probation, canonicalIp, payload ?? string.Empty, now));
                return true;
            }
            database.ExecuteInTransaction((connection, transaction) =>
            {
                connection.Execute("INSERT INTO ThreatHubJournal(EventType,SourceIp,Payload,CreatedTicks) VALUES(@type,@ip,@json,@ticks)",
                    new { type = ThreatHubJournalEventType.Probation, ip = canonicalIp, json = payload ?? string.Empty, ticks = now }, transaction);
            });
            return true;
        }
    }

    /// <summary>
    /// 依據序號游標讀取威脅情資異動日誌增量記錄。
    /// </summary>
    /// <param name="cursor">起始序號游標（僅讀取 Sequence 大於此游標之記錄）。</param>
    /// <param name="limit">最大讀取筆數（預設 256 筆）。</param>
    /// <returns>異動日誌項目清單。</returns>
    internal IReadOnlyList<ThreatHubJournalEntry> ReadJournal(long cursor, int limit = 256)
    {
        if (limit <= 0) limit = PageSize;
        lock (gate)
        {
            if (database is null)
            {
                return memoryJournal
                    .Where(j => j.Sequence > cursor)
                    .OrderBy(j => j.Sequence)
                    .Take(limit)
                    .ToList();
            }
            return database.Query<ThreatHubJournalEntry>(
                "SELECT Sequence, EventType, SourceIp, Payload, CreatedTicks FROM ThreatHubJournal WHERE Sequence > @cursor ORDER BY Sequence LIMIT @limit",
                new { cursor, limit }).ToList();
        }
    }

    internal ThreatHubSyncResponse ReadPage(long cursor, string generation)
    {
        lock (gate)
        {
            string currentGeneration = Generation;
            if (generation != currentGeneration || cursor < 0) cursor = 0;
            long now = DateTime.UtcNow.Ticks;
            List<Entry> entries = database is null
                ? memory.Values.Where(e => e.Sequence > cursor && e.ExpiresTicks > now).OrderBy(e => e.Sequence).Take(PageSize + 1).ToList()
                : database.Query<Entry>("SELECT Sequence,Payload,ExpiresTicks FROM ThreatHubEntries WHERE Sequence>@cursor AND ExpiresTicks>@now ORDER BY Sequence LIMIT 257", new { cursor, now }).ToList();
            bool hasMore = entries.Count > PageSize;
            if (hasMore) entries.RemoveAt(PageSize);
            return new ThreatHubSyncResponse
            {
                Generation = currentGeneration,
                NextCursor = entries.Count == 0 ? cursor : entries[^1].Sequence,
                HasMore = hasMore,
                ActiveThreats = entries.Select(e => JsonSerializer.Deserialize<ThreatIntelligenceItem>(e.Payload)!).ToList()
            };
        }
    }

    /// <summary>
    /// 取得最新入庫之活動威脅情資清單。
    /// </summary>
    /// <param name="count">最多取得筆數。</param>
    /// <returns>最新威脅清單。</returns>
    internal IReadOnlyList<ThreatIntelligenceItem> GetRecentThreats(int count = 10)
    {
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                return memory.Values
                    .Where(e => e.ExpiresTicks > now)
                    .OrderByDescending(e => e.Sequence)
                    .Take(count)
                    .Select(e => JsonSerializer.Deserialize<ThreatIntelligenceItem>(e.Payload))
                    .Where(t => t != null)!
                    .ToList()!;
            }
            var rows = database.Query<Entry>("SELECT Sequence,Payload,ExpiresTicks FROM ThreatHubEntries WHERE ExpiresTicks>@now ORDER BY Sequence DESC LIMIT @count", new { now, count });
            List<ThreatIntelligenceItem> list = [];
            foreach (var r in rows)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<ThreatIntelligenceItem>(r.Payload);
                    if (item != null) list.Add(item);
                }
                catch (JsonException) { }
                catch (NotSupportedException) { }
            }
            return list;
        }
    }

    /// <summary>
    /// 快速查詢特定 IP 是否存在於活動威脅情資庫中。
    /// </summary>
    /// <param name="ip">欲查詢之 IP 位址。</param>
    /// <returns>若存在傳回威脅項目；否則傳回 <see langword="null"/>。</returns>
    internal ThreatIntelligenceItem? LookupThreat(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                if (memory.TryGetValue(ip, out Entry? entry) && entry.ExpiresTicks > now)
                    return JsonSerializer.Deserialize<ThreatIntelligenceItem>(entry.Payload);
                return null;
            }
            string? payload = database.QueryFirstOrDefault<string>("SELECT Payload FROM ThreatHubEntries WHERE SourceIp=@ip AND ExpiresTicks>@now", new { ip, now });
            return !string.IsNullOrEmpty(payload) ? JsonSerializer.Deserialize<ThreatIntelligenceItem>(payload) : null;
        }
    }

    /// <summary>
    /// 統計威脅情資來源組成（外部訂閱情報 vs 叢集邊緣節點回報）。
    /// </summary>
    /// <returns>外部訂閱數與叢集回報數之元組。</returns>
    internal (int ExternalFeedCount, int ClusterNodeCount) GetFeedComposition()
    {
        long now = DateTime.UtcNow.Ticks;
        lock (gate)
        {
            if (database is null)
            {
                int ext = 0;
                int cluster = 0;
                foreach (var e in memory.Values.Where(e => e.ExpiresTicks > now))
                {
                    try
                    {
                        var item = JsonSerializer.Deserialize<ThreatIntelligenceItem>(e.Payload);
                        if (item == null || string.IsNullOrWhiteSpace(item.ReporterNodeId)) ext++;
                        else cluster++;
                    }
                    catch (JsonException) { ext++; }
                    catch (NotSupportedException) { ext++; }
                }
                return (ext, cluster);
            }
            object? extObj = database.ExecuteScalar(
                "SELECT COUNT(*) FROM ThreatHubEntries WHERE ExpiresTicks>@p0 AND (Payload LIKE '%\"ReporterNodeId\":\"\"%' OR Payload LIKE '%\"ReporterNodeId\":null%')",
                now);
            int externalCount = extObj != null && int.TryParse(extObj.ToString(), out int parsedExt) ? parsedExt : 0;
            int total = ActiveThreatCount;
            int clusterCount = Math.Max(0, total - externalCount);
            return (externalCount, clusterCount);
        }
    }

    private sealed class Entry
    {
        public long Sequence { get; set; }
        public string Payload { get; set; } = string.Empty;
        public long ExpiresTicks { get; set; }
    }
}

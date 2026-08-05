using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// Verifies durable protection-control evidence and bounded JSON export.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ProtectionAuditTrailTest
{
    private string testDirectory = null!;
    private Database database = null!;

    /// <summary>
    /// Creates an isolated database for each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "IDDSCommunity.AuditTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "audit.db");
    }

    /// <summary>
    /// Releases the database and temporary files after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);
    }

    /// <summary>
    /// Verifies that protection evidence is persisted, queried, and exported without SQL or JSON injection.
    /// </summary>
    /// <returns>A task that completes after the export is read.</returns>
    [TestMethod]
    public async Task RecordAndExportJsonAsync_PersistsStructuredEvidence()
    {
        ProtectionAuditTrail trail = new(database, TimeProvider.System);
        DateTimeOffset fromUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        trail.Record("Firewall.HardLock", "Succeeded", "DOMAIN\\service", "192.0.2.10", "agent\"one");

        var records = await trail.ReadAsync(fromUtc, DateTimeOffset.UtcNow.AddMinutes(1)).ConfigureAwait(false);
        using MemoryStream output = new();
        await trail.ExportJsonAsync(output, fromUtc, DateTimeOffset.UtcNow.AddMinutes(1)).ConfigureAwait(false);
        string json = Encoding.UTF8.GetString(output.ToArray());
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.HasCount(1, records);
        Assert.AreEqual("Firewall.HardLock", records[0].EventType);
        Assert.AreEqual("192.0.2.10", records[0].Subject);
        Assert.AreEqual("agent\"one", document.RootElement[0].GetProperty("Details").GetString());
    }

    /// <summary>
    /// Verifies that invalid or unbounded evidence queries are rejected.
    /// </summary>
    [TestMethod]
    public void ReadAsync_InvalidWindowOrLimit_Throws()
    {
        ProtectionAuditTrail trail = new(database, TimeProvider.System);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => trail.ReadAsync(now, now));
        Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => trail.ReadAsync(now.AddMinutes(-1), now, 10001));
    }

    /// <summary>
    /// Verifies that retention removes only evidence older than the approved period.
    /// </summary>
    /// <returns>A task that completes after retained evidence is queried.</returns>
    [TestMethod]
    public async Task PurgeOlderThanAsync_RemovesOnlyExpiredEvidence()
    {
        ManualTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ProtectionAuditTrail trail = new(database, timeProvider);
        trail.Record("Runtime.Start", "Succeeded", "service", "server01");
        timeProvider.UtcNow = timeProvider.UtcNow.AddDays(400);
        trail.Record("Runtime.Stop", "Succeeded", "service", "server01");

        int deleted = await trail.PurgeOlderThanAsync(TimeSpan.FromDays(365)).ConfigureAwait(false);
        var records = await trail.ReadAsync(timeProvider.UtcNow.AddDays(-366), timeProvider.UtcNow.AddDays(1)).ConfigureAwait(false);

        Assert.AreEqual(1, deleted);
        Assert.HasCount(1, records);
        Assert.AreEqual("Runtime.Stop", records[0].EventType);
    }

    /// <summary>
    /// Verifies that configuration persistence records keys but never sensitive values.
    /// </summary>
    /// <returns>A task that completes after the audit event is queried.</returns>
    [TestMethod]
    public async Task SaveAppConfig_RecordsChangedKeyWithoutValue()
    {
        IddsConfig configuration = new(database);
        configuration.LoadAppConfig();
        configuration.SetConfigValue("Smtp.Password", "not-for-audit");
        configuration.SaveAppConfig();
        ProtectionAuditTrail trail = new(database, TimeProvider.System);

        var records = await trail.ReadAsync(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1)).ConfigureAwait(false);

        Assert.HasCount(1, records);
        Assert.AreEqual("Configuration.Change", records[0].EventType);
        Assert.AreEqual("Smtp.Password", records[0].Subject);
        Assert.AreEqual(string.Empty, records[0].Details);
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = utcNow;

        /// <summary>
        /// Gets the deterministic UTC time used by the retention test.
        /// </summary>
        /// <returns>The configured UTC time.</returns>
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

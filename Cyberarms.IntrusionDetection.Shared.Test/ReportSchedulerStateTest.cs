using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class ReportSchedulerStateTest
{
    /// <summary>
    /// Verifies that a failed delivery persists failure without advancing its checkpoint and can later recover.
    /// </summary>
    /// <returns>A task that completes after retry succeeds.</returns>
    [TestMethod]
    public async Task CheckDailyReportAsync_WhenDeliveryFails_PersistsFailureAndRetries()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CyberarmsTests", Guid.NewGuid().ToString("N"));
        Database database = new();
        try
        {
            database.Configure(directory);
            IddsConfig configuration = new(database) { ApplicationPath = directory };
            configuration.LoadAppConfig();
            NotificationSettings settings = new(configuration);
            ReportScheduler scheduler = new(new FixedTimeProvider(new DateTimeOffset(2026, 8, 5, 1, 0, 0, TimeSpan.Zero)), settings);
            scheduler.RunDailyReportAsync += _ => Task.FromException(new InvalidOperationException("delivery failed"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => scheduler.CheckDailyReportAsync()).ConfigureAwait(false);
            Assert.AreEqual(ReportDeliveryState.Failed, settings.DailyReportState);
            Assert.AreNotEqual("2026-8-4", settings.LastDailyReport);

            scheduler = new ReportScheduler(new FixedTimeProvider(new DateTimeOffset(2026, 8, 5, 1, 0, 0, TimeSpan.Zero)), settings);
            scheduler.RunDailyReportAsync += _ => Task.CompletedTask;
            await scheduler.CheckDailyReportAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(ReportDeliveryState.Succeeded, settings.DailyReportState);
            Assert.AreEqual("2026-8-4", settings.LastDailyReport);
        }
        finally
        {
            database.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        /// <summary>
        /// Gets the deterministic UTC time used by the scheduler test.
        /// </summary>
        /// <returns>The configured time.</returns>
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

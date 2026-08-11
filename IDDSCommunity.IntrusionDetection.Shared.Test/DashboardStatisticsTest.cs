using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class DashboardStatisticsTest
{
    [TestMethod]
    public void FailedLoginStatisticsUsesOneHalfOpenWindowForTotalAndAgents()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-count-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddDays(30);
            Guid firstAgent = Guid.NewGuid();
            Guid secondAgent = Guid.NewGuid();

            InsertIncident(database, start, firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddDays(1), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL);
            InsertIncident(database, start.AddDays(2), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE);
            InsertIncident(database, end.AddTicks(-1), secondAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddDays(3), firstAgent, IntrusionLog.STATUS_SOFT_LOCKED);
            InsertIncident(database, start.AddTicks(-1), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, end, secondAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);

            Assert.AreEqual(4, snapshot.Total);
            Assert.AreEqual(3, snapshot.AttemptsByAgent[firstAgent]);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[secondAgent]);
            Assert.AreEqual(snapshot.Total, snapshot.AttemptsByAgent.Values.Sum());
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); } catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void FailedLoginStatisticsRejectsAnEmptyOrReversedWindow()
    {
        DateTime boundary = new(2026, 8, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Locks.ReadFailedLoginStatistics(boundary, boundary));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Locks.ReadFailedLoginStatistics(boundary, boundary.AddTicks(-1)));
    }

    private static void InsertIncident(Database database, DateTime time, Guid agentId, int action) =>
        database.ExecuteNonQuery(
            "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
            time,
            agentId,
            "192.0.2.10",
            action);
}

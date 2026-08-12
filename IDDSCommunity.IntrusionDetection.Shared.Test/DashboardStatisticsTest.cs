using System;
using System.IO;
using System.Collections.Generic;
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
            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,SoftLocks,HardLocks) VALUES(@p0,999,4,2)",
                firstAgent);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);
            IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatistics = Locks.ReadAgentLockStatistics();

            Assert.AreEqual(4, snapshot.Total);
            Assert.AreEqual(3, snapshot.AttemptsByAgent[firstAgent]);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[secondAgent]);
            Assert.AreEqual(snapshot.Total, snapshot.AttemptsByAgent.Values.Sum());
            Assert.AreEqual(2, lockStatistics[firstAgent].HardLocks);
            Assert.AreEqual(4, lockStatistics[firstAgent].SoftLocks);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
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

    [TestMethod]
    public void GroupedSecurityLogUsesHalfOpenWindowAndExactFailureStatuses()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-security-log-window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            Assert.AreEqual(1L, Db.DbValueConverter.ToInt64(database.ExecuteScalar(
                "select count(*) from sqlite_master where type='index' and name='IX_IntrusionLog_IncidentTime'")));
            DateTime start = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddHours(24);
            Guid agentId = Guid.NewGuid();
            InsertIncident(database, start, agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, end.AddTicks(-1), agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddHours(1), agentId, 101);
            InsertIncident(database, end, agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            int total = 0;
            using System.Data.IDataReader reader = IntrusionLog.ReadIntervalGrouped(start, end);
            while (reader.Read())
                total += Db.DbValueConverter.ToInt(reader["NumberOfEvents"]);

            Assert.AreEqual(3, total);
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT));
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL));
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE));
            Assert.IsFalse(IntrusionLog.IsFailedLoginAction(101));
            Assert.IsFalse(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_SOFT_LOCKED));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT,
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL,
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE
                },
                IntrusionLog.FailedLoginActions.ToArray());
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void InsertIncident(Database database, DateTime time, Guid agentId, int action) =>
        database.ExecuteNonQuery(
            "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
            time,
            agentId,
            "192.0.2.10",
            action);
}

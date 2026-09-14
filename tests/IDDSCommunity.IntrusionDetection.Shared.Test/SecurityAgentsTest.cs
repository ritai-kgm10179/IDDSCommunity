using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class SecurityAgentsTest
{
    [TestMethod]
    public void GetDisplayName_WhenDatabaseLookupFails_LogsAndReturnsFallback()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-agent-name-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        StringWriter traceOutput = new();
        TextWriterTraceListener listener = new(traceOutput);
        try
        {
            database.Configure(directory);
            IddsConfig configuration = new(database);
            SecurityAgents agents = new(database, configuration);
            database.ExecuteNonQuery("DROP TABLE SecurityAgents");
            Trace.Listeners.Add(listener);

            string result = agents.GetDisplayName("missing-agent");
            Trace.Flush();

            StringAssert.Contains(result, "missing-agent");
            StringAssert.Contains(traceOutput.ToString(), "Unable to resolve an Agent display name");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
            listener.Dispose();
            traceOutput.Dispose();
            database.Close();
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [TestMethod]
    public void MergeDbInformation_WhenAgentAlreadyExistsInDatabase_DoesNotTriggerUnnecessarySave()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-merge-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            IddsConfig configuration = new(database);
            SecurityAgents agents = new(database, configuration);

            Guid agentId = Guid.NewGuid();
            SecurityAgent initial = new("TestAgent", agentId)
            {
                DisplayName = "Test Agent",
                DatabaseInstance = database
            };
            initial.Save();
            int originalSerial = initial.Serial;

            agents.InitializeAgents();
            Assert.AreEqual(1, agents.Count);

            SecurityAgent diskDiscovered = new("TestAgent", agentId)
            {
                DisplayName = "Test Agent",
                AssemblyFilename = "IDDSCommunity.Agents.Test.dll"
            };

            agents.MergeDbInformation([diskDiscovered]);

            int postMergeSerial = Convert.ToInt32(database.ExecuteScalar("SELECT Serial FROM SecurityAgents WHERE AgentId = @p0", agentId.ToString()));
            Assert.AreEqual(originalSerial, postMergeSerial);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); } catch { }
        }
    }
}

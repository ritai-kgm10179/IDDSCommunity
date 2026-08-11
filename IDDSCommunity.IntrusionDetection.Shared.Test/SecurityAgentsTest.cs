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
}

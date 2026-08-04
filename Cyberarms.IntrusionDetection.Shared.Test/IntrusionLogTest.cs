using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public class IntrusionLogTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntrusionLogTest"/> class.
    /// </summary>

    public IntrusionLogTest() => Database.Instance.Configure(System.Windows.Forms.Application.StartupPath);

    /// <summary>
    /// Reads interval test.
    /// </summary>

    [TestMethod]
    public void ReadIntervalTest()
    {
        prepareIntrusionLog();
        IDataReader rdr = IntrusionLog.ReadInterval(new TimeSpan(0, 24, 0, 0, 0));
        if (rdr.FieldCount != 6) Assert.Fail("Field count changed!");

        while (rdr.Read())
        {
            System.Diagnostics.Debug.Print("Log Id {0} ({1}): {2}", rdr["Id"], rdr["IncidentTime"], rdr["ClientIP"]);
        }

    }

    /// <summary>
    /// Determines whether s updates test.
    /// </summary>

    [TestMethod]
    public void HasUpdatesTest()
    {
    }

    /// <summary>
    /// Reads differential test.
    /// </summary>

    [TestMethod]
    public void ReadDifferentialTest()
    {
    }

    /// <summary>
    /// Executes the prepare intrusion log operation.
    /// </summary>

    private static void prepareIntrusionLog()
    {
        Database.Instance.ExecuteNonQuery(INSERT_COMMAND, DateTime.Now.AddHours(-1), DBNull.Value, "10.10.1.1", 0, false);
        Database.Instance.ExecuteNonQuery(INSERT_COMMAND, DateTime.Now.AddHours(-1).AddMinutes(-1), DBNull.Value, "10.10.1.1", 0, false);
        Database.Instance.ExecuteNonQuery(INSERT_COMMAND, DateTime.Now.AddHours(-1).AddMinutes(-2), DBNull.Value, "10.10.1.1", 0, false);
        Database.Instance.ExecuteNonQuery(INSERT_COMMAND, DateTime.Now.AddHours(-1).AddMinutes(-3), DBNull.Value, "10.10.1.1", 0, false);
        Database.Instance.ExecuteNonQuery(INSERT_COMMAND, DateTime.Now.AddHours(-1).AddMinutes(-4), DBNull.Value, "10.10.1.1", 0, false);

    }

    const string INSERT_COMMAND = "insert into IntrusionLog(IncidentTime,AgentId, ClientIP, Action, ActionTriggeredByUser) values(@p0,@p1,@p2,@p3,@p4)";


}

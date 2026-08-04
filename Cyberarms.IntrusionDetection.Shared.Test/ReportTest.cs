using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Cyberarms.IntrusionDetection.Shared.Test;

/// <summary>
/// Summary description for ReportTest
/// </summary>
[TestClass]
public class ReportTest
{
    public ReportTest()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    private TestContext testContextInstance;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext
    {
        get => testContextInstance; set => testContextInstance = value;
    }

    #region Additional test attributes
    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    [TestMethod]
    public void TestReportByAgent()
    {
        Database.Instance.Configure(AppDomain.CurrentDomain.BaseDirectory);
        string result = ReportGenerator.Instance.GetEventsPerAgent(DateTime.Now.AddDays(-3), DateTime.Now);
        System.Diagnostics.Debug.Print(result);
    }

    [TestMethod]
    public void TestReport()
    {
        Database.Instance.Configure(AppDomain.CurrentDomain.BaseDirectory);
        string ipAddresses = string.Empty;

        System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (System.Net.IPAddress ip in host.AddressList)
        {
            ipAddresses += ip.ToString() + "<br/>";
        }

        string result = ReportGenerator.Instance.GetReport("Last three days report", "This report contains data of the last three days and is for testing only. If you got this report without running the unit test before, data might be outdated.",
            string.Format(@"Server name: {0} <br/>
                        IP addresses: <br/>{1}<br/>IDDS Version: {2}", System.Net.Dns.GetHostName(), ipAddresses, "Pro Edition"), DateTime.Now.AddDays(-3), DateTime.Now);
        if (!Directory.Exists(@"c:\temp"))
        {
            try { Directory.CreateDirectory(@"c:\temp"); } catch { }
        }
        StreamWriter sw = new(@"c:\temp\testreportrun.htm");
        sw.WriteLine(result);
        sw.Close();
        try
        {
            System.Diagnostics.ProcessStartInfo sInfo = new(@"c:\temp\testreportrun.htm") { UseShellExecute = true };
            System.Diagnostics.Process.Start(sInfo);
        }
        catch { }
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Xml.Serialization;

namespace IDDSCommunity.IntrusionDetection.Service.Test;
/// <summary>
/// Summary description for ApiTest
/// </summary>
[TestClass]
public class ApiTest
{
    private TestContext testContextInstance = null!;
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
    /// <summary>
    /// 執行 test serialization 作業。
    /// </summary>

    [TestMethod]
    public void TestSerialization()
    {
        TestPluginConfig config = new()
        {
            Prop1 = "Test1",
            Prop2 = "Test2"
        };
        XmlSerializer xs = new(typeof(TestPluginConfig));
        string outputPath = System.IO.Path.Combine(TestContext.TestRunDirectory ?? TestContext.DeploymentDirectory ?? System.AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N") + "-pluginsettings.xml");
        using System.IO.StreamWriter sw = new(outputPath);
        xs.Serialize(sw, config);
        sw.Close();
        if (System.IO.File.Exists(outputPath)) System.IO.File.Delete(outputPath);
    }


}


public class TestPluginConfig : PluginConfiguration
{
    public string Prop1 { get; set; } = string.Empty;
    public string Prop2 { get; set; } = string.Empty;
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public class DatabaseUpgradeTest
{
    /// <summary>
    /// Executes the test database creation operation.
    /// </summary>

    [TestMethod]
    public void TestDatabaseCreation()
    {
        Database.Instance.Configure("c:\\temp");
        Assert.AreEqual(1, Database.Instance.DatabaseVersion);

    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Cyberarms.IntrusionDetection.Service.Test;

[TestClass]
public class EventLogTest
{
    [TestMethod]
    public void TestCreateWhenSourceExists()
    {
        try
        {
            WindowsLogManager.Instance.WriteEntry("Test Message", EventLogEntryType.Information, 0, 0);
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

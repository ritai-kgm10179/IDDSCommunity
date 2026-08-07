using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.Test;
/// <summary>
/// Summary description for LockTest
/// </summary>
[TestClass]
public class LockTest
{
    /// <summary>
    /// 初始化 <see cref="LockTest"/> 類別的新執行個體。
    /// </summary>

    public LockTest()
    {
        //
        // TODO: Add constructor logic here
        //
    }

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
    /// 執行 test ip address local 作業。
    /// </summary>

    [TestMethod]
    public void TestIpAddressLocal()
    {
        IddsConfig.Instance.ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
        var ip = IPAddress.Parse("127.0.0.1");
        Assert.IsTrue(IddsConfig.Instance.IsIpAddressLocal(ip));
        foreach (IPAddress address in getLocalIps())
        {
            Assert.IsTrue(IddsConfig.Instance.IsIpAddressLocal(address));
            System.Diagnostics.Debug.Print(address.ToString());
        }
        Assert.IsFalse(IddsConfig.Instance.IsIpAddressLocal(IPAddress.Parse("10.1.1.1")));
        Assert.IsFalse(IddsConfig.Instance.IsIpAddressLocal(IPAddress.Parse("192.168.13.1")));
        Assert.IsFalse(IddsConfig.Instance.IsIpAddressLocal(IPAddress.Parse("73.24.12.42")));
    }
    /// <summary>
    /// 執行 test is ip address local performance test 作業。
    /// </summary>

    [TestMethod]
    public void TestIsIpAddressLocalPerformanceTest()
    {
        DateTime start = DateTime.Now;
        for (int i = 0; i < 2000; i++)
        {
            TestIpAddressLocal();
        }
        if ((DateTime.Now - start).TotalSeconds > 1)
        {
            Assert.Fail("Time taken for 28.000 ip address comparisons: " + (DateTime.Now - start).TotalSeconds + " seconds!");
        }
    }


    private List<IPAddress>? _localAddresses;
    /// <summary>
    /// 執行 get local ips 作業。
    /// </summary>
    /// <returns>傳回 get local ips 的結果。</returns>

    private List<IPAddress> getLocalIps()
    {
        if (_localAddresses == null)
        {
            _localAddresses = [];
            foreach (System.Net.NetworkInformation.NetworkInterface iface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                System.Net.NetworkInformation.IPInterfaceProperties iprop = iface.GetIPProperties();
                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation info in iprop.UnicastAddresses)
                {
                    _localAddresses.Add(IPAddress.Parse(info.Address.ToString()));
                }
            }
        }
        return _localAddresses;
    }

}

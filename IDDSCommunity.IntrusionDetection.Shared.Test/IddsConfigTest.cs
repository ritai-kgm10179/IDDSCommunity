using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public class IddsConfigTest
{
    private string TestDirectory => TestContext.TestRunDirectory ?? TestContext.DeploymentDirectory ?? AppDomain.CurrentDomain.BaseDirectory;

    public TestContext TestContext { get; set; } = null!;
    /// <summary>
    /// Configures test database.
    /// </summary>

    [TestInitialize]
    public void ConfigureTestDatabase()
    {
        Database.Instance.Configure(TestDirectory);
        IddsConfig.Instance.ApplicationPath = TestDirectory;
    }
    /// <summary>
    /// Saves config test.
    /// </summary>

    [TestMethod]
    public void SaveConfigTest()
    {
        IddsConfig.Instance.ConfigVersionNumber = 1;
        IddsConfig.Instance.CyberSheriffContributor = true;
        IddsConfig.Instance.Edition = "PRO";
        IddsConfig.Instance.Expires = DateTime.MaxValue;
        IddsConfig.Instance.HardLockAttempts = 10;
        IddsConfig.Instance.HardLockTimeHours = 2;
        IddsConfig.Instance.LockForever = false;
        IddsConfig.Instance.NotificationEmailAddress = "maxemilian.hilbrand@readytomarket.net";
        IddsConfig.Instance.SenderEmailAddress = "idds@localhost";
        IddsConfig.Instance.SendInfoMail = true;
        IddsConfig.Instance.SmtpPassword = "smtpPasss";
        IddsConfig.Instance.SmtpPort = 25;
        IddsConfig.Instance.SmtpRequiresAuthentication = false;
        IddsConfig.Instance.SmtpServer = "localhost";
        IddsConfig.Instance.SmtpUsername = "smtpuser";
        IddsConfig.Instance.SoftLockAttempts = 3;
        IddsConfig.Instance.SoftLockTimeMinutes = 20;
        IddsConfig.Instance.UseSafeNetworkList = true;
        IddsConfig.Instance.WebBasedMonitoring = true;

        IddsConfig.Instance.Save();

    }
    /// <summary>
    /// Verifies that new installations use the reviewed two-stage source-IP throttling defaults.
    /// </summary>
    [TestMethod]
    public void DefaultLockoutConfigurationUsesReviewedThresholdsTest()
    {
        IddsConfig configuration = IddsConfig.GetDefaultConfiguration();

        Assert.AreEqual(10, configuration.SoftLockAttempts);
        Assert.AreEqual(1, configuration.SoftLockTimeMinutes);
        Assert.AreEqual(20, configuration.HardLockAttempts);
        Assert.AreEqual(1, configuration.HardLockTimeHours);
    }

    //[TestMethod]
    //public void CreateAgentConfigTest() {
    //    IddsConfig.PluginDirectory = "test";
    //    IddsConfig.Instance.ApplicationPath = @"c:\\temp\\";
    //    IDDSCommunity.IntrusionDetection.Api.Plugin.AgentConfigurationBase agentConfig = new IDDSCommunity.IntrusionDetection.Api.Plugin.AgentConfigurationBase();
    //    agentConfig.AgentName = "TestAgent";
    //    agentConfig.AssemblyName = "TestDom.TestAgent";
    //    agentConfig.ConfigurationSettingsTypeName = "ConfigType";
    //    agentConfig.Enabled = false;
    //    agentConfig.FileName = "TestAgent.dll";
    //    agentConfig.HardLockAttempts = 100;
    //    agentConfig.HardLockDurationHrs = 10;
    //    agentConfig.NeverUnlock = true;
    //    agentConfig.OverwriteConfiguration = true;
    //    agentConfig.PluginConfigurationXml = "<xml>";
    //    agentConfig.SoftLockAttempts = 20;
    //    agentConfig.SoftLockDurationMins = 200;
    //    //IddsConfig.Instance.WriteAgentConfiguration(agentConfig);

    //}


    /// <summary>
    /// Reads write app config test.
    /// </summary>

    [TestMethod]
    public void ReadWriteAppConfigTest()
    {
        IddsConfig.Instance.GetConfigValue("TestConfigSetting1");
        IddsConfig.Instance.SetConfigValue("TestConfigSetting1", "Value1");
        IddsConfig.Instance.SetConfigValue("TestConfigSetting2", "Value2");
        IddsConfig.Instance.SaveAppConfig();
        IddsConfig.Instance.AppConfig.Clear();
        IddsConfig.Instance.LoadAppConfig();
        Assert.AreEqual("Value1", IddsConfig.Instance.GetConfigValue("TestConfigSetting1"));
        Assert.AreEqual("Value2", IddsConfig.Instance.GetConfigValue("TestConfigSetting2"));
        IddsConfig.Instance.AppConfig.Remove("TestConfigSetting1");
        IddsConfig.Instance.AppConfig.Remove("TestConfigSetting2");
        IddsConfig.Instance.SaveAppConfig();
    }
    /// <summary>
    /// Verifies that firewall mode is persisted and unsupported values safely use inbound blocking.
    /// </summary>
    [TestMethod]
    public void FirewallBlockModePersistsAndRejectsUnsupportedValuesTest()
    {
        IddsConfig.Instance.FirewallBlockMode = FirewallBlockMode.Bidirectional;
        IddsConfig.Instance.SaveAppConfig();
        IddsConfig.Instance.LoadAppConfig();
        Assert.AreEqual(FirewallBlockMode.Bidirectional, IddsConfig.Instance.FirewallBlockMode);

        IddsConfig.Instance.SetConfigValue(IddsConfig.CONFIG_VALUE_FIREWALL_BLOCK_MODE, "BlackholeRouting");
        Assert.AreEqual(FirewallBlockMode.Inbound, IddsConfig.Instance.FirewallBlockMode);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            IddsConfig.Instance.FirewallBlockMode = (FirewallBlockMode)999);

        IddsConfig.Instance.FirewallBlockMode = FirewallBlockMode.Inbound;
        IddsConfig.Instance.SaveAppConfig();
    }
    /// <summary>
    /// 執行 config is in safe network test 作業。
    /// </summary>

    [TestMethod]
    public void ConfigIsInSafeNetworkTest()
    {
        IddsConfig.Instance.SafeNetworks.Add(new IddsConfig.CSafeNetwork("192.168.1.1", "255.255.255.255"));
        Assert.IsTrue(IddsConfig.Instance.IsInSafeNetwork("192.168.1.1"));
        Assert.IsFalse(IddsConfig.Instance.IsInSafeNetwork("192.168.1.2"));
        IddsConfig.Instance.SafeNetworks.Add(new IddsConfig.CSafeNetwork("192.168.1.0", "255.255.255.0"));
        Assert.IsTrue(IddsConfig.Instance.IsInSafeNetwork("192.168.1.1"));
        Assert.IsTrue(IddsConfig.Instance.IsInSafeNetwork("192.168.1.2"));
    }
    /// <summary>
    /// Verifies that SMTP credentials use protected storage and remain decryptable.
    /// </summary>
    [TestMethod]
    public void SmtpPasswordUsesProtectedStorageTest()
    {
        const string password = "Correct Horse Battery Staple";

        IddsConfig.Instance.SetSmtpPassword(password);

        Assert.AreNotEqual(password, IddsConfig.Instance.SmtpPassword);
        StringAssert.StartsWith(IddsConfig.Instance.SmtpPassword, "dpapi:v1:");
        Assert.AreEqual(password, IddsConfig.Instance.GetSmtpPassword());
    }
    /// <summary>
    /// Verifies prefix matching and rejection of noncontiguous IPv4 subnet masks.
    /// </summary>
    [TestMethod]
    public void SubnetMatchingValidatesPrefixTest()
    {
        System.Net.IPAddress network = System.Net.IPAddress.Parse("192.168.16.0");

        Assert.IsTrue(IddsConfig.IsIpInNetwork(System.Net.IPAddress.Parse("192.168.31.255"), network, 20, 4));
        Assert.IsFalse(IddsConfig.IsIpInNetwork(System.Net.IPAddress.Parse("192.168.32.1"), network, 20, 4));
        Assert.AreEqual(20, IddsConfig.GetSubnetMaskBits("255.255.240.0"));
        Assert.IsFalse(IddsConfig.IsValidSubnetMask("255.0.255.0"));
    }

    [TestMethod]
    public void SafeNetworkInputNormalizesIpv4AndIpv6RangesTest()
    {
        Assert.AreEqual("192.0.2.1/255.255.255.255", IddsConfig.ConvertStringToIpAddressNetwork("192.0.2.1"));
        Assert.AreEqual("192.0.2.0/255.255.255.0", IddsConfig.ConvertStringToIpAddressNetwork("192.0.2.0/24"));
        Assert.AreEqual("0.0.0.0/0.0.0.0", IddsConfig.ConvertStringToIpAddressNetwork("0.0.0.0/0"));
        Assert.AreEqual("::1/128", IddsConfig.ConvertStringToIpAddressNetwork("::1"));
        Assert.AreEqual("2001:db8::/32", IddsConfig.ConvertStringToIpAddressNetwork("2001:db8::/32"));
        Assert.ThrowsExactly<ArgumentException>(() => IddsConfig.ConvertStringToIpAddressNetwork("2001:db8::/129"));

        IddsConfig.Instance.SafeNetworks.Clear();
        IddsConfig.Instance.SafeNetworks.Add(new IddsConfig.CSafeNetwork("2001:db8::", "32"));
        Assert.IsTrue(IddsConfig.Instance.IsInSafeNetwork("2001:db8:1::20"));
        Assert.IsFalse(IddsConfig.Instance.IsInSafeNetwork("2001:db9::20"));
    }
    /// <summary>
    /// Verifies that validation exceptions remain diagnostic and culture-independent.
    /// </summary>
    [TestMethod]
    public void InvalidNetworkExceptionIsNotLocalizedTest()
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");
            ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
                () => IddsConfig.ConvertStringToIpAddressNetwork("not-an-address"));

            StringAssert.Contains(exception.Message, "InvalidIpAddress");
            Assert.AreEqual("ipAddressNetwork", exception.ParamName);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
    /// <summary>
    /// Verifies that asynchronous database operations use an independently owned connection.
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task DatabaseAsyncOperationsTest()
    {
        long value = await Database.Instance.ExecuteScalarAsync<long>("SELECT 42");

        Assert.AreEqual(42L, value);
    }
}

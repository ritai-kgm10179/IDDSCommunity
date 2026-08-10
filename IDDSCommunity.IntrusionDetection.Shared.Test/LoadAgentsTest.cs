using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[Serializable]
[TestClass]
[DoNotParallelize]
public class LoadAgentsTest
{
    private string testDirectory = null!;

    public TestContext TestContext { get; set; } = null!;
    /// <summary>
    /// 初始化 <see cref="LoadAgentsTest"/> 類別的新執行個體。
    /// </summary>
    public LoadAgentsTest()
    {
        Database.Instance.Configure(System.Windows.Forms.Application.StartupPath);
    }

    [TestInitialize]
    public void Initialize()
    {
        string root = TestContext.TestRunDirectory ?? Path.GetTempPath();
        testDirectory = Path.Combine(root, nameof(LoadAgentsTest), TestContext.TestName);
        Directory.CreateDirectory(testDirectory);
        IddsConfig.Instance.PluginsDirectory = testDirectory;
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, recursive: true);
    }
    /// <summary>
    /// Loads agents from directory.
    /// </summary>

    [TestMethod]
    public void LoadAgentsFromDirectory()
    {
        List<SecurityAgent> agents = SecurityAgents.Instance.ReadAgentsFromDisk();
        foreach (SecurityAgent agent in agents)
        {
            System.Diagnostics.Debug.Print(agent.Name);
        }
    }

    [TestMethod]
    public void ReadAgentsFromDiskIgnoresLegacyAndUnrelatedAssemblies()
    {
        File.WriteAllBytes(Path.Combine(testDirectory, "Unrelated.Plugin.dll"), [0x00]);

        List<SecurityAgent> agents = SecurityAgents.Instance.ReadAgentsFromDisk();

        Assert.HasCount(0, agents);
    }
    /// <summary>
    /// Merges disk agents with db.
    /// </summary>

    [TestMethod]
    public void MergeDiskAgentsWithDbPreservesStoredStateAndDisablesNewAgents()
    {
        SecurityAgents agents = new(Database.Instance, IddsConfig.Instance);
        Guid existingId = Guid.NewGuid();
        SecurityAgent stored = new("Existing.Agent", existingId) { Enabled = true, DisplayName = "Stored display name" };
        agents.Add(stored);
        SecurityAgent discoveredExisting = new("Existing.Agent", existingId) { AssemblyFilename = "IDDSCommunity.Agents.Existing.dll", DisplayName = "Discovered display name" };
        discoveredExisting.CustomConfigurationTypes["ReadEventLog"] = typeof(bool).FullName!;
        SecurityAgent discoveredNew = new("New.Agent", Guid.NewGuid()) { AssemblyFilename = "IDDSCommunity.Agents.New.dll", Enabled = true };

        List<SecurityAgent> merged = agents.MergeDbInformation([discoveredExisting, discoveredNew]);

        Assert.HasCount(2, merged);
        Assert.AreSame(stored, merged[0]);
        Assert.IsTrue(stored.Enabled);
        Assert.IsFalse(stored.BinaryMissing);
        Assert.AreEqual(discoveredExisting.AssemblyFilename, stored.AssemblyFilename);
        Assert.AreEqual(discoveredExisting.DisplayName, stored.DisplayName);
        Assert.AreEqual(typeof(bool).FullName, stored.CustomConfigurationTypes["ReadEventLog"]);
        Assert.IsFalse(merged[1].Enabled);
        Assert.AreEqual(discoveredNew.Id, merged[1].Id);
    }

    /// <summary>
    /// 驗證同一組件內的多個 Agent 不會僅因組件名稱相同而互相套用設定。
    /// </summary>
    [TestMethod]
    public void MergeDbInformation_DoesNotMatchDifferentAgentsByAssemblyName()
    {
        SecurityAgents agents = new(Database.Instance, IddsConfig.Instance);
        SecurityAgent storedSmtp = new("Legacy.SmtpAgent", Guid.NewGuid())
        {
            AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
            DisplayName = "舊 SMTP Agent",
            Enabled = true
        };
        agents.Add(storedSmtp);
        SecurityAgent discoveredPop3 = new("IDDSCommunity.Agents.MailServer.Pop3Agent", Guid.NewGuid())
        {
            AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
            DisplayName = "POP3 安全性代理程式",
            Enabled = true
        };

        List<SecurityAgent> merged = agents.MergeDbInformation([discoveredPop3]);

        Assert.HasCount(2, merged);
        Assert.IsTrue(storedSmtp.BinaryMissing);
        Assert.IsFalse(discoveredPop3.Enabled);
    }

    /// <summary>
    /// 驗證已載入目前設定結構的 Agent 會忽略並清除舊版殘留欄位。
    /// </summary>
    [TestMethod]
    public void CustomConfigurationSchema_FiltersAndDeletesStaleProperties()
    {
        string databaseDirectory = Path.Combine(testDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(databaseDirectory);
        Database database = new();
        database.Configure(databaseDirectory, "agent-schema.db");
        Guid agentId = Guid.NewGuid();
        SecurityAgent smtp = new("IDDSCommunity.Agents.MailServer.SmtpAgent", agentId)
        {
            DatabaseInstance = database,
            AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
            DisplayName = "郵件伺服器 SMTP 安全性代理程式"
        };
        smtp.CustomConfiguration["SmtpPort"] = "25";
        smtp.CustomConfigurationTypes["SmtpPort"] = typeof(int).FullName!;
        smtp.Save();
        database.ExecuteNonQuery(
            "insert into SecurityAgentConfig(AgentId,PropertyName,PropertyValueString) values(@p0,@p1,@p2)",
            agentId.ToString(), "Pop3Port", "110");

        smtp.LoadCustomConfig();

        Assert.HasCount(1, smtp.CustomConfiguration);
        Assert.AreEqual("25", smtp.CustomConfiguration["SmtpPort"]);
        Assert.IsFalse(smtp.CustomConfiguration.ContainsKey("Pop3Port"));

        smtp.SaveCustomConfig();
        Assert.AreEqual(0L, Convert.ToInt64(database.ExecuteScalar(
            "select count(*) from SecurityAgentConfig where AgentId=@p0 and PropertyName=@p1",
            agentId.ToString(), "Pop3Port")));
        database.Close();
    }
    /// <summary>
    /// Loads agents to memory test.
    /// </summary>

    [TestMethod]
    public void LoadAgentsToMemoryCreatesAndUnloadsProxy()
    {
        string pluginDirectory = FindBuiltPluginDirectory();
        IddsConfig configuration = new(Database.Instance) { PluginsDirectory = pluginDirectory };
        SecurityAgents agents = new(Database.Instance, configuration);
        SecurityAgent agent = agents.ReadAgentsFromDisk().First(item => item.Name != "DemoAgent.BadAgent");
        agent.Id = Guid.Empty;
        agent.Enabled = true;
        agents.Add(agent);

        agents.LoadAgents();

        Assert.HasCount(1, agents.LoadedAgents);
        AgentProxy proxy = agents.LoadedAgents[agent];
        Assert.IsNotNull(proxy.Configuration);
        agents.UnloadAgents();
        Assert.HasCount(0, agents.LoadedAgents);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = proxy.Configuration);
    }

    [TestMethod]
    public void ApplyCustomConfigurationSupportsBooleanIntegerAndStringValues()
    {
        TestPluginConfiguration configuration = new();

        SecurityAgents.ApplyCustomConfiguration(configuration, new Dictionary<string, string>
        {
            [nameof(TestPluginConfiguration.Enabled)] = "False",
            [nameof(TestPluginConfiguration.Port)] = "2222",
            [nameof(TestPluginConfiguration.Path)] = @"C:\Logs\agent.log"
        });

        Assert.IsFalse(configuration.Enabled);
        Assert.AreEqual(2222, configuration.Port);
        Assert.AreEqual(@"C:\Logs\agent.log", configuration.Path);
    }

    /// <summary>
    /// 驗證 Agent 勾選啟用並儲存後，模擬應用程式關閉並重新啟動載入，Enabled 狀態 100% 完整保留。
    /// </summary>
    [TestMethod]
    public void SaveAndReopen_PreservesEnabledStateAcrossAppRestarts()
    {
        string testDbDir = Path.Combine(Path.GetTempPath(), "IDDS_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDbDir);
        try
        {
            Database testDb = new();
            testDb.Configure(testDbDir);
            IddsConfig testConfig = new(testDb) { PluginsDirectory = testDbDir };
            SecurityAgents agents = new(testDb, testConfig);

            Guid agentId = Guid.NewGuid();
            SecurityAgent originalAgent = new("Test.Persistence.Agent", agentId)
            {
                DisplayName = "Test Persistence Agent",
                Enabled = true,
                AssemblyName = "IDDSCommunity.Agents.Test.dll"
            };
            originalAgent.DatabaseInstance = testDb;
            originalAgent.Save();

            testDb.Close();
            testDb.Configure(testDbDir);

            SecurityAgents reloadedAgents = new(testDb, testConfig);
            reloadedAgents.InitializeAgents();

            SecurityAgent? loaded = reloadedAgents.FindByName("Test.Persistence.Agent");
            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.Enabled, "Agent Enabled state must remain True after reopening app!");
        }
        finally
        {
            try { Directory.Delete(testDbDir, true); } catch { }
        }
    }

    /// <summary>
    /// 驗證包含 int (Port 9833)、string (Path) 與 bool 等所有自訂屬性型態在儲存並重開資料庫後，100% 正確還原。
    /// </summary>
    [TestMethod]
    public void SaveAndReopen_CustomProperties_AllTypes_PersistedCorrectly()
    {
        string testDbDir = Path.Combine(Path.GetTempPath(), "IDDS_Test_Types_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDbDir);
        try
        {
            Database testDb = new();
            testDb.Configure(testDbDir);
            IddsConfig testConfig = new(testDb) { PluginsDirectory = testDbDir };

            Guid agentId = Guid.NewGuid();
            SecurityAgent originalAgent = new("Test.Types.Agent", agentId)
            {
                DisplayName = "Test Types Agent",
                Enabled = true,
                AssemblyName = "IDDSCommunity.Agents.Test.dll"
            };
            originalAgent.DatabaseInstance = testDb;
            originalAgent.CustomConfiguration["Port"] = "9833";
            originalAgent.CustomConfiguration["Path"] = @"C:\Logs\idds.log";
            originalAgent.CustomConfiguration["EnableFeature"] = "1";
            originalAgent.Save();

            testDb.Close();
            testDb.Configure(testDbDir);

            SecurityAgents reloadedAgents = new(testDb, testConfig);
            reloadedAgents.InitializeAgents();

            SecurityAgent? loaded = reloadedAgents.FindByName("Test.Types.Agent");
            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.Enabled);
            Assert.AreEqual("9833", loaded.CustomConfiguration["Port"]);
            Assert.AreEqual(@"C:\Logs\idds.log", loaded.CustomConfiguration["Path"]);
            Assert.AreEqual("1", loaded.CustomConfiguration["EnableFeature"]);

            TestPluginConfiguration config = new();
            SecurityAgents.ApplyCustomConfiguration(config, loaded.CustomConfiguration);
            Assert.AreEqual(9833, config.Port);
            Assert.AreEqual(@"C:\Logs\idds.log", config.Path);
            Assert.IsTrue(config.Enabled);
        }
        finally
        {
            try { Directory.Delete(testDbDir, true); } catch { }
        }
    }

    /// <summary>
    /// 測試 MailServer 下的 3 個 Agent (IMAP, POP3, SMTP) 具備獨一無二的 Guid，可同時啟用且不互相覆蓋。
    /// </summary>
    [TestMethod]
    public void MailServerAgents_HaveUniqueGuids_AndCanAllBeEnabledSimultaneously()
    {
        string testDbDir = Path.Combine(Path.GetTempPath(), "idds_test_mail_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDbDir);
        try
        {
            Database testDb = new();
            testDb.Configure(testDbDir);

            SecurityAgent imap = new()
            {
                Id = new Guid("{3F8B715C-4A2D-4C98-9C6E-7F89B219E022}"),
                Name = "IDDSCommunity.Agents.MailServer.ImapAgent",
                DisplayName = "IMAP Security Agent",
                AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
                Enabled = true,
                DatabaseInstance = testDb
            };

            SecurityAgent pop3 = new()
            {
                Id = new Guid("{1F917251-2661-473A-970B-B2BB62EA6E1A}"),
                Name = "IDDSCommunity.Agents.MailServer.Pop3Agent",
                DisplayName = "POP3 Security Agent",
                AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
                Enabled = true,
                DatabaseInstance = testDb
            };

            SecurityAgent smtp = new()
            {
                Id = new Guid("{EB69BF23-939C-4F89-97D0-50274306D018}"),
                Name = "IDDSCommunity.Agents.MailServer.SmtpAgent",
                DisplayName = "Mail Server SMTP Security Agent",
                AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
                Enabled = true,
                DatabaseInstance = testDb
            };

            Assert.AreNotEqual(imap.Id, pop3.Id);
            Assert.AreNotEqual(pop3.Id, smtp.Id);
            Assert.AreNotEqual(imap.Id, smtp.Id);

            imap.Save();
            pop3.Save();
            smtp.Save();

            SecurityAgents agents = new(testDb, IddsConfig.Instance);
            agents.InitializeAgents();

            Assert.AreEqual(3, agents.Count);
            Assert.IsTrue(agents.FindByName("IDDSCommunity.Agents.MailServer.ImapAgent")!.Enabled);
            Assert.IsTrue(agents.FindByName("IDDSCommunity.Agents.MailServer.Pop3Agent")!.Enabled);
            Assert.IsTrue(agents.FindByName("IDDSCommunity.Agents.MailServer.SmtpAgent")!.Enabled);
        }
        finally
        {
            try { Directory.Delete(testDbDir, true); } catch { }
        }
    }

    /// <summary>
    /// 驗證依序開啟 SMTP、IMAP 與 POP3 後，每筆啟用狀態均以各自 GUID 儲存且重新載入後不互相覆蓋。
    /// </summary>
    [TestMethod]
    public void MailServerAgents_SequentialEnableAndReload_PreservesAllThreeEnabledStates()
    {
        string databaseDirectory = Path.Combine(testDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(databaseDirectory);
        Database database = new();
        database.Configure(databaseDirectory, "mail-agents.db");
        SecurityAgent[] mailAgents =
        [
            CreateMailAgent(new Guid("{EB69BF23-939C-4F89-97D0-50274306D018}"), "SmtpAgent", "郵件伺服器 SMTP 安全性代理程式", database),
            CreateMailAgent(new Guid("{3F8B715C-4A2D-4C98-9C6E-7F89B219E022}"), "ImapAgent", "IMAP 安全性代理程式", database),
            CreateMailAgent(new Guid("{1F917251-2661-473A-970B-B2BB62EA6E1A}"), "Pop3Agent", "POP3 安全性代理程式", database)
        ];

        foreach (SecurityAgent agent in mailAgents)
        {
            agent.Enabled = true;
            agent.Save();
        }

        SecurityAgents reloaded = new(database, IddsConfig.Instance);
        reloaded.InitializeAgents();

        Assert.HasCount(3, reloaded);
        foreach (SecurityAgent agent in mailAgents)
        {
            SecurityAgent? persisted = reloaded.FindByName(agent.Name);
            Assert.IsNotNull(persisted);
            Assert.AreEqual(agent.Id, persisted.Id);
            Assert.IsTrue(persisted.Enabled, $"{agent.Name} 應保持啟用。 ");
        }
        Assert.AreEqual(3L, Convert.ToInt64(database.ExecuteScalar("select count(*) from SecurityAgents where Enabled=1")));
        database.Close();
    }

    private static SecurityAgent CreateMailAgent(Guid id, string typeName, string displayName, Database database) => new()
    {
        Id = id,
        Name = "IDDSCommunity.Agents.MailServer." + typeName,
        AssemblyName = "IDDSCommunity.Agents.MailServer.dll",
        DisplayName = displayName,
        DatabaseInstance = database
    };

    /// <summary>
    /// 測試 GetDisplayName 支援大小寫不敏感之 Guid 比對。
    /// </summary>
    [TestMethod]
    public void GetDisplayName_CaseInsensitiveGuid_ReturnsCorrectDisplayName()
    {
        string testDbDir = Path.Combine(Path.GetTempPath(), "idds_test_disp_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDbDir);
        try
        {
            Database testDb = new();
            testDb.Configure(testDbDir);
            SecurityAgents agents = new(testDb, IddsConfig.Instance);

            Guid targetId = new("{A682433B-852F-4150-ADF4-FB7F75090015}");
            SecurityAgent rdpAgent = new()
            {
                Id = targetId,
                Name = "IDDSCommunity.Agents.TerminalServer.TlsSslAgent",
                DisplayName = "RDP / Terminal Server Agent",
                DatabaseInstance = testDb
            };
            agents.Add(rdpAgent);

            string upperGuid = targetId.ToString().ToUpperInvariant();
            string lowerGuid = targetId.ToString().ToLowerInvariant();

            Assert.AreEqual("RDP / Terminal Server Agent", agents.GetDisplayName(upperGuid));
            Assert.AreEqual("RDP / Terminal Server Agent", agents.GetDisplayName(lowerGuid));
        }
        finally
        {
            try { Directory.Delete(testDbDir, true); } catch { }
        }
    }

    private sealed class TestPluginConfiguration : IntrusionDetection.Api.Plugin.PluginConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int Port { get; set; } = 1;
        public string Path { get; set; } = string.Empty;
    }

    private static string FindBuiltPluginDirectory()
    {
        DirectoryInfo frameworkDirectory = new(AppContext.BaseDirectory);
        string configuration = frameworkDirectory.Parent?.Name ?? throw new DirectoryNotFoundException();
        DirectoryInfo? root = frameworkDirectory;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "IDDSCommunity.slnx"))) root = root.Parent;
        if (root is null) throw new DirectoryNotFoundException("Repository root was not found.");
        string pluginDirectory = Path.Combine(root.FullName, "IDDSCommunity.IntrusionDetection.Admin", "bin", configuration, frameworkDirectory.Name, "Plugins");
        if (!Directory.Exists(pluginDirectory)) throw new DirectoryNotFoundException(pluginDirectory);
        return pluginDirectory;
    }

}

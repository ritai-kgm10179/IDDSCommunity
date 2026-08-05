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
    /// Initializes a new instance of the <see cref="LoadAgentsTest"/> class.
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
        File.WriteAllBytes(Path.Combine(testDirectory, "Cyberarms.Agents.Legacy.dll"), [0x00]);
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
        SecurityAgent discoveredNew = new("New.Agent", Guid.NewGuid()) { AssemblyFilename = "IDDSCommunity.Agents.New.dll", Enabled = true };

        List<SecurityAgent> merged = agents.MergeDbInformation([discoveredExisting, discoveredNew]);

        Assert.HasCount(2, merged);
        Assert.AreSame(stored, merged[0]);
        Assert.IsTrue(stored.Enabled);
        Assert.IsFalse(stored.BinaryMissing);
        Assert.AreEqual(discoveredExisting.AssemblyFilename, stored.AssemblyFilename);
        Assert.AreEqual(discoveredExisting.DisplayName, stored.DisplayName);
        Assert.IsFalse(merged[1].Enabled);
        Assert.AreEqual(discoveredNew.Id, merged[1].Id);
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

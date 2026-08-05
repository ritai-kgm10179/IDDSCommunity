using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[Serializable]
[TestClass]
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

    [TestMethod, Ignore]
    public void MergeDiskAgentsWithDb()
    {
        SecurityAgents.Instance.Add(new SecurityAgent("SmtpAgent", Guid.NewGuid(), 0, 0, 0, null!));
        SecurityAgents.Instance[0].Enabled = true;
        List<SecurityAgent> diskAgents = SecurityAgents.Instance.ReadAgentsFromDisk();
        List<SecurityAgent> agents = SecurityAgents.Instance.MergeDbInformation(diskAgents);
        foreach (SecurityAgent agent in agents)
        {
            System.Diagnostics.Debug.Print(agent.DisplayName);
        }
        if (agents.Count > 1) Assert.IsFalse(agents[1].Enabled);
    }

    /// <summary>
    /// Loads agents to memory test.
    /// </summary>

    [TestMethod, Ignore]
    public void LoadAgentsToMemoryTest()
    {
        SecurityAgents.Instance.Add(new SecurityAgent("SmtpAgent", Guid.NewGuid(), 0, 0, 0, null!));
        SecurityAgents.Instance[0].Enabled = true;
        List<SecurityAgent> diskAgents = SecurityAgents.Instance.ReadAgentsFromDisk();
        SecurityAgents.Instance.MergeDbInformation(diskAgents);
        SecurityAgents.Instance[1].Enabled = true;
        SecurityAgents.Instance.LoadAgents();
        foreach (SecurityAgent key in SecurityAgents.Instance.LoadedAgents.Keys)
        {
            SecurityAgents.Instance.LoadedAgents[key].AttackDetected += new Api.Plugin.AttackDetectedHandler(LoadAgentsTest_AttackDetected);
        }
        SecurityAgents.Instance[1].AppDomain.DomainUnload += new EventHandler(AppDomain_DomainUnload);
        SecurityAgents.Instance.UnloadAgents();
        System.Timers.Timer t = new(1000);
        t.Elapsed += new System.Timers.ElapsedEventHandler(t_Elapsed);
        t.Enabled = true;
        t.Start();
        while (!Finished)
        {
            System.Threading.Thread.SpinWait(10);
        }
        // Assert.IsTrue(Unloaded); // just works in debug, because object is released too early in runtime
    }

    /// <summary>
    /// Handles the elapsed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void t_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) => Finished = true;
    public bool Finished { get; set; }
    public bool Unloaded { get; set; }

    /// <summary>
    /// Handles the domain unload event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void AppDomain_DomainUnload(object? sender, EventArgs e) => Unloaded = true;

    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    void LoadAgentsTest_AttackDetected(object sender, Api.Plugin.INotificationEventArgs data) => System.Diagnostics.Debug.Print("Attack detected");


}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;
/// <summary>
/// Verifies that every production Agent project participates in Admin debug builds and shared deployment.
/// </summary>
[TestClass]
public sealed class PluginDeploymentTest
{
    /// <summary>
    /// Verifies that the Admin project references and deploys every production Agent project.
    /// </summary>
    [TestMethod]
    public void AdminBuildIncludesEveryProductionAgentPlugin()
    {
        string root = FindRepositoryRoot();
        string[] expected = GetProductionAgentProjectNames(root);

        XDocument adminProject = XDocument.Load(Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Admin", "IDDSCommunity.IntrusionDetection.Admin.csproj"));
        string[] referenced = adminProject.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include")))
            .Where(name => name?.StartsWith("IDDSCommunity.Agents.", StringComparison.Ordinal) is true)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

        XDocument buildTargets = XDocument.Load(Path.Combine(root, "Directory.Build.targets"));
        string[] deployed = (buildTargets.Descendants("IDDSCommunityAgentPluginProjects").Single().Value)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expected, referenced, "Admin Debug build references are incomplete.");
        CollectionAssert.AreEqual(expected, deployed, "Shared Plugin deployment list is incomplete.");
    }
    /// <summary>
    /// Verifies that the current Admin build output contains every Plugin and that each assembly is discoverable.
    /// </summary>
    [TestMethod]
    public void AdminOutputContainsEveryLoadableProductionAgentPlugin()
    {
        string root = FindRepositoryRoot();
        DirectoryInfo targetFrameworkDirectory = new(AppContext.BaseDirectory);
        string configuration = targetFrameworkDirectory.Parent?.Name
            ?? throw new DirectoryNotFoundException("The test configuration directory was not found.");
        string pluginDirectory = Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Admin", "bin", configuration, targetFrameworkDirectory.Name, "Plugins");
        string[] expected = GetProductionAgentProjectNames(root);
        string[] actual = Directory.EnumerateFiles(pluginDirectory, "IDDSCommunity.Agents.*.dll", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual, "Admin output does not contain every production Plugin.");
        AgentLoaderProxy loader = new();
        foreach (string assemblyPath in Directory.EnumerateFiles(pluginDirectory, "IDDSCommunity.Agents.*.dll", SearchOption.TopDirectoryOnly))
            Assert.IsGreaterThan(0, loader.GetSecurityAgents(assemblyPath, pluginDirectory).Count, $"No Agent was discovered in {Path.GetFileName(assemblyPath)}.");
    }
    /// <summary>
    /// Verifies that Plugin-provided display keys are localized at the shared loading boundary.
    /// </summary>
    [TestMethod]
    public void AgentLoaderLocalizesEveryPluginDisplayKey()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Shared", "AgentLoaderProxy.cs"));

        StringAssert.Contains(source, "Localization.Strings.Get(exInfo.DisplayName)");
        StringAssert.Contains(source, "Localization.Strings.Get(typeName)");
    }
    /// <summary>
    /// Verifies that the administration UI runs unelevated and requests elevation only for service state changes.
    /// </summary>
    [TestMethod]
    public void AdminUsesOnDemandElevationForServiceCommands()
    {
        string root = FindRepositoryRoot();
        string adminDirectory = Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Admin");
        string manifest = File.ReadAllText(Path.Combine(adminDirectory, "ProtectionConfig.exe.manifest"));
        string commandSource = File.ReadAllText(Path.Combine(adminDirectory, "ElevatedServiceCommand.cs"));
        string adminSource = File.ReadAllText(Path.Combine(adminDirectory, "IddsAdmin.cs"));

        StringAssert.Contains(manifest, "level=\"asInvoker\"");
        Assert.DoesNotContain("requireAdministrator", manifest, StringComparison.Ordinal);
        StringAssert.Contains(commandSource, "Verb = \"runas\"");
        StringAssert.Contains(adminSource, "ElevatedServiceCommand.RunElevatedAsync");
    }
    /// <summary>
    /// Gets all production Agent project names while excluding test and removed project folders.
    /// </summary>
    /// <param name="root">The repository root.</param>
    /// <returns>傳回 sorted production Agent project names 的結果。</returns>
    private static string[] GetProductionAgentProjectNames(string root) =>
        Directory.EnumerateDirectories(Path.Combine(root, "agents"), "IDDSCommunity.Agents.*", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".Test", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".Common", StringComparison.OrdinalIgnoreCase))
            .Where(path => Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();
    /// <summary>
    /// Finds the repository root from the test output directory.
    /// </summary>
    /// <returns>傳回 repository root 的結果。</returns>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IDDSCommunity.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("IDDSCommunity repository root was not found.");
    }
}

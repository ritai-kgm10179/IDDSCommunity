using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class PluginPathValidatorTest
{
    /// <summary>
    /// Verifies that a DLL directly beneath the trusted root is accepted and normalized.
    /// </summary>
    [TestMethod]
    public void Validate_WhenDllIsInsideRoot_ReturnsFullPath()
    {
        using TemporaryPluginTree tree = new();
        string plugin = tree.CreateFile("agent.dll");

        Assert.AreEqual(Path.GetFullPath(plugin), PluginPathValidator.Validate(tree.Root, plugin));
    }

    /// <summary>
    /// Verifies that an assembly outside the trusted root is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenPathEscapesRoot_ThrowsUnauthorizedAccessException()
    {
        using TemporaryPluginTree tree = new();
        string outside = Path.Combine(Path.GetDirectoryName(tree.Root)!, "outside.dll");
        File.WriteAllBytes(outside, []);
        try
        {
            Assert.ThrowsExactly<UnauthorizedAccessException>(() => PluginPathValidator.Validate(tree.Root, outside));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    /// <summary>
    /// Verifies that non-DLL files cannot be loaded as Agent assemblies.
    /// </summary>
    [TestMethod]
    public void Validate_WhenExtensionIsNotDll_ThrowsInvalidOperationException()
    {
        using TemporaryPluginTree tree = new();
        string plugin = tree.CreateFile("agent.txt");

        Assert.ThrowsExactly<InvalidOperationException>(() => PluginPathValidator.Validate(tree.Root, plugin));
    }

    private sealed class TemporaryPluginTree : IDisposable
    {
        internal TemporaryPluginTree()
        {
            Root = Path.Combine(Path.GetTempPath(), "CyberarmsTests", Guid.NewGuid().ToString("N"), "Plugins");
            Directory.CreateDirectory(Root);
        }

        internal string Root { get; }

        /// <summary>
        /// Creates an empty test file beneath the trusted root.
        /// </summary>
        /// <param name="fileName">The test file name.</param>
        /// <returns>The created path.</returns>
        internal string CreateFile(string fileName)
        {
            string path = Path.Combine(Root, fileName);
            File.WriteAllBytes(path, []);
            return path;
        }

        /// <summary>
        /// Removes the temporary plug-in tree.
        /// </summary>
        public void Dispose()
        {
            string testRoot = Directory.GetParent(Root)!.FullName;
            if (Directory.Exists(testRoot))
                Directory.Delete(testRoot, recursive: true);
        }
    }
}

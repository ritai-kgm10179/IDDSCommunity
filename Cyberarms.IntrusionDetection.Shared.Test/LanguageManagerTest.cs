using System.Globalization;
using Cyberarms.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public class LanguageManagerTest
{
    /// <summary>
    /// Executes the test default culture fallback operation.
    /// </summary>

    [TestMethod]
    public void TestDefaultCultureFallback()
    {
        LanguageManager.Instance.Initialize("auto");
        // Ensure default culture fallback returns en-US or zh-TW
        Assert.IsTrue(LanguageManager.Instance.CurrentCulture.Name == "en-US" || LanguageManager.Instance.CurrentCulture.Name == "zh-TW");
    }

    /// <summary>
    /// Executes the test explicit culture set operation.
    /// </summary>

    [TestMethod]
    public void TestExplicitCultureSet()
    {
        LanguageManager.Instance.Initialize("zh-TW");
        Assert.AreEqual("zh-TW", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("Cyberarms 入侵防禦系統", Strings.AppTitle);

        LanguageManager.Instance.Initialize("en");
        Assert.AreEqual("en-US", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("Cyberarms Intrusion Detection", Strings.AppTitle);
    }

    /// <summary>
    /// Executes the test unsupported culture fallback operation.
    /// </summary>

    [TestMethod]
    public void TestUnsupportedCultureFallback()
    {
        // When user provides unsupported culture (e.g. ja-JP, fr-FR), fallback to en-US
        LanguageManager.Instance.Initialize("ja-JP");
        Assert.AreEqual("en-US", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("Cyberarms Intrusion Detection", Strings.AppTitle);
    }

    /// <summary>
    /// Executes the test get string fallback operation.
    /// </summary>

    [TestMethod]
    public void TestGetStringFallback()
    {
        LanguageManager.Instance.Initialize("en");
        string val = LanguageManager.Instance.GetString("NonExistentKey", "DefaultFallback");
        Assert.AreEqual("DefaultFallback", val);
    }

    /// <summary>
    /// Verifies that neutral and Traditional Chinese resources expose identical keys.
    /// </summary>
    [TestMethod]
    public void ResourceCulturesHaveMatchingKeys()
    {
        string root = FindRepositoryRoot();
        string neutralPath = Path.Combine(root, "Cyberarms.IntrusionDetection.Shared", "Localization", "Strings.resx");
        string traditionalChinesePath = Path.Combine(root, "Cyberarms.IntrusionDetection.Shared", "Localization", "Strings.zh-TW.resx");
        AssertResourceKeysAreUnique(neutralPath);
        AssertResourceKeysAreUnique(traditionalChinesePath);
        HashSet<string> neutral = LoadResourceKeys(neutralPath);
        HashSet<string> traditionalChinese = LoadResourceKeys(traditionalChinesePath);
        CollectionAssert.AreEquivalent(neutral.ToArray(), traditionalChinese.ToArray());
    }

    /// <summary>
    /// Verifies that production sources do not reintroduce hard-coded user-facing messages.
    /// </summary>
    [TestMethod]
    public void ProductionSourcesDoNotContainHardCodedUserFacingMessages()
    {
        string root = FindRepositoryRoot();
        Regex forbidden = new("throw new [A-Za-z0-9_.<>]+Exception\\(\\s*\"|MessageBox\\.Show\\(\\s*\"|Console\\.Write(?:Line)?\\(\\s*\"|WindowsLogManager\\.Instance\\.WriteEntry\\(\\s*\"|GenericErrorDialog(?:\\s+[A-Za-z0-9_]+\\s*=)?\\s*new\\(\\s*\"|EventMessage\\s*=\\s*\"", RegexOptions.Compiled);
        Regex designerText = new("\\.(?:Text|HeaderText|ToolTipText)\\s*=\\s*\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                    && !Regex.IsMatch(path, @"\\[^\\]*(?:\.Test|Test)\\", RegexOptions.IgnoreCase)))
        {
            string source = File.ReadAllText(file);
            if (source.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Any(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal) && forbidden.IsMatch(line)))
            {
                violations.Add(Path.GetRelativePath(root, file));
            }
            foreach (Match match in designerText.Matches(source))
            {
                string value = match.Groups["value"].Value;
                if (value is not ("" or "0" or "|")) violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.AreEqual(0, violations.Distinct(StringComparer.OrdinalIgnoreCase).Count(), string.Join(Environment.NewLine, violations.Distinct()));
    }

    /// <summary>
    /// Verifies that literal keys passed to the localization API exist in both resource cultures.
    /// </summary>
    [TestMethod]
    public void LocalizedCallsReferenceExistingResources()
    {
        string root = FindRepositoryRoot();
        HashSet<string> keys = LoadResourceKeys(Path.Combine(root, "Cyberarms.IntrusionDetection.Shared", "Localization", "Strings.resx"));
        Regex localizedCall = new("Strings\\.(?:Get|Format)\\(\\s*\"(?<key>[^\"]+)\"", RegexOptions.Compiled);
        List<string> missing = [];
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            string source = File.ReadAllText(file);
            foreach (Match match in localizedCall.Matches(source))
            {
                string key = Regex.Unescape(match.Groups["key"].Value);
                if (!keys.Contains(key)) missing.Add($"{Path.GetRelativePath(root, file)}: {key}");
            }
        }
        Assert.AreEqual(0, missing.Count, string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// Loads resource keys from a resx file.
    /// </summary>
    /// <param name="path">The resx path.</param>
    /// <returns>The unique resource keys.</returns>
    private static HashSet<string> LoadResourceKeys(string path) => XDocument.Load(path).Root!.Elements("data")
        .Select(element => (string)element.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Rejects duplicate resource names, including names that differ only by casing.
    /// </summary>
    /// <param name="path">The resx path.</param>
    private static void AssertResourceKeysAreUnique(string path)
    {
        string[] names = XDocument.Load(path).Root!.Elements("data")
            .Select(element => (string)element.Attribute("name")!).ToArray();
        string[] duplicates = names.GroupBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1).Select(static group => group.Key).ToArray();
        Assert.AreEqual(0, duplicates.Length, string.Join(Environment.NewLine, duplicates));
    }

    /// <summary>
    /// Finds the repository root from the test output directory.
    /// </summary>
    /// <returns>The repository root path.</returns>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cyberarms.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Cyberarms repository root was not found.");
    }
}

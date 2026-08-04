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

    [TestMethod]
    public void ResourceCulturesHaveMatchingKeys()
    {
        string root = FindRepositoryRoot();
        HashSet<string> neutral = LoadResourceKeys(Path.Combine(root, "Cyberarms.IntrusionDetection.Shared", "Localization", "Strings.resx"));
        HashSet<string> traditionalChinese = LoadResourceKeys(Path.Combine(root, "Cyberarms.IntrusionDetection.Shared", "Localization", "Strings.zh-TW.resx"));
        CollectionAssert.AreEquivalent(neutral.ToArray(), traditionalChinese.ToArray());
    }

    [TestMethod]
    public void ProductionSourcesDoNotContainHardCodedUserFacingMessages()
    {
        string root = FindRepositoryRoot();
        Regex forbidden = new("throw new [A-Za-z0-9_.<>]+Exception\\(\\s*\"|MessageBox\\.Show\\(\\s*\"|Console\\.Write(?:Line)?\\(\\s*\"|WindowsLogManager\\.Instance\\.WriteEntry\\(\\s*\"", RegexOptions.Compiled);
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
            if (!file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (Match match in designerText.Matches(source))
            {
                string value = match.Groups["value"].Value;
                if (value is not ("" or "0" or "|")) violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.AreEqual(0, violations.Distinct(StringComparer.OrdinalIgnoreCase).Count(), string.Join(Environment.NewLine, violations.Distinct()));
    }

    private static HashSet<string> LoadResourceKeys(string path) => XDocument.Load(path).Root!.Elements("data")
        .Select(element => (string)element.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cyberarms.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Cyberarms repository root was not found.");
    }
}

using System.Globalization;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public class LanguageManagerTest
{
    /// <summary>
    /// 執行 test default culture fallback 作業。
    /// </summary>

    [TestMethod]
    public void TestDefaultCultureFallback()
    {
        LanguageManager.Instance.Initialize("auto");
        // Ensure default culture fallback returns en-US or zh-TW
        Assert.IsTrue(LanguageManager.Instance.CurrentCulture.Name == "en-US" || LanguageManager.Instance.CurrentCulture.Name == "zh-TW");
    }
    /// <summary>
    /// 執行 test explicit culture set 作業。
    /// </summary>

    [TestMethod]
    public void TestExplicitCultureSet()
    {
        LanguageManager.Instance.Initialize("zh-TW");
        Assert.AreEqual("zh-TW", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("IDDS 社群版", Strings.AppTitle);

        LanguageManager.Instance.Initialize("en");
        Assert.AreEqual("en-US", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("IDDS Community", Strings.AppTitle);
    }

    /// <summary>
    /// 驗證恢復預設值確認提示提供完整的英文與正體中文翻譯。
    /// </summary>
    [TestMethod]
    public void RestoreDefaultsConfirmation_IsLocalizedInSupportedCultures()
    {
        LanguageManager.Instance.Initialize("zh-TW");
        Assert.AreEqual("確認恢復預設值", Strings.Get("Confirm restore defaults"));
        Assert.AreEqual(
            "要將此頁面的設定恢復為預設值嗎？尚未儲存的變更將被取代。",
            Strings.Get("Restore the settings on this page to their defaults? Unsaved changes will be replaced."));

        LanguageManager.Instance.Initialize("en-US");
        Assert.AreEqual("Confirm restore defaults", Strings.Get("Confirm restore defaults"));
        Assert.AreEqual(
            "Restore the settings on this page to their defaults? Unsaved changes will be replaced.",
            Strings.Get("Restore the settings on this page to their defaults? Unsaved changes will be replaced."));
    }
    /// <summary>
    /// 驗證通知信主旨與內文提供完整的英文與正體中文在地化支援，並符合全形標點與盤古之白規範。
    /// </summary>
    [TestMethod]
    public void NotificationEmail_Localization_FormatsProperlyInChineseAndEnglish()
    {
        LanguageManager.Instance.Initialize("zh-TW");
        Assert.AreEqual("IDDS 社群版：解除封鎖通知 (192.168.1.100)", Strings.Format("IDDS Community: Unlock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("IDDS 社群版：軟封鎖通知 (192.168.1.100)", Strings.Format("IDDS Community: Soft lock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("IDDS 社群版：硬封鎖通知 (192.168.1.100)", Strings.Format("IDDS Community: Hard lock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("IP 位址為 192.168.1.100 之用戶端已被硬封鎖（永久封鎖）。", Strings.Format("Client with IP address {0} was hard locked.", "192.168.1.100"));
        Assert.AreEqual("IP 位址為 192.168.1.100 之用戶端已被軟封鎖。", Strings.Format("Client with IP address {0} was soft locked.", "192.168.1.100"));
        Assert.AreEqual("IP 位址為 192.168.1.100 之用戶端已被解除封鎖。", Strings.Format("Client with IP address {0} was unlocked.", "192.168.1.100"));
        Assert.AreEqual("嘗試硬封鎖 IP 位址為 192.168.1.100 之用戶端時發生錯誤：\r\nAccess denied", Strings.Format("Error while trying to hard lock client with IP address {0}:\r\n{1}", "192.168.1.100", "Access denied"));
        Assert.AreEqual("嘗試軟封鎖 IP 位址為 192.168.1.100 之用戶端時發生錯誤：\r\nAccess denied", Strings.Format("Error while trying to soft lock client with IP address {0}:\r\n{1}", "192.168.1.100", "Access denied"));

        LanguageManager.Instance.Initialize("en-US");
        Assert.AreEqual("IDDS Community: Unlock notification (192.168.1.100)", Strings.Format("IDDS Community: Unlock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("IDDS Community: Soft lock notification (192.168.1.100)", Strings.Format("IDDS Community: Soft lock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("IDDS Community: Hard lock notification (192.168.1.100)", Strings.Format("IDDS Community: Hard lock notification ({0})", "192.168.1.100"));
        Assert.AreEqual("Client with IP address 192.168.1.100 was hard locked.", Strings.Format("Client with IP address {0} was hard locked.", "192.168.1.100"));
        Assert.AreEqual("Client with IP address 192.168.1.100 was soft locked.", Strings.Format("Client with IP address {0} was soft locked.", "192.168.1.100"));
        Assert.AreEqual("Client with IP address 192.168.1.100 was unlocked.", Strings.Format("Client with IP address {0} was unlocked.", "192.168.1.100"));
        Assert.AreEqual("Error while trying to hard lock client with IP address 192.168.1.100:\r\nAccess denied", Strings.Format("Error while trying to hard lock client with IP address {0}:\r\n{1}", "192.168.1.100", "Access denied"));
        Assert.AreEqual("Error while trying to soft lock client with IP address 192.168.1.100:\r\nAccess denied", Strings.Format("Error while trying to soft lock client with IP address {0}:\r\n{1}", "192.168.1.100", "Access denied"));
    }
    /// <summary>
    /// 執行 test unsupported culture fallback 作業。
    /// </summary>

    [TestMethod]
    public void TestUnsupportedCultureFallback()
    {
        // When user provides unsupported culture (e.g. ja-JP, fr-FR), fallback to en-US
        LanguageManager.Instance.Initialize("ja-JP");
        Assert.AreEqual("en-US", LanguageManager.Instance.CurrentCulture.Name);
        Assert.AreEqual("IDDS Community", Strings.AppTitle);
    }
    /// <summary>
    /// 執行 test get string fallback 作業。
    /// </summary>

    [TestMethod]
    public void TestGetStringFallback()
    {
        LanguageManager.Instance.Initialize("en");
        string val = LanguageManager.Instance.GetString("NonExistentKey", "DefaultFallback");
        Assert.AreEqual("DefaultFallback", val);
    }
    /// <summary>
    /// 驗證中性語系與正體中文資源提供完全相同的資源鍵。
    /// </summary>
    [TestMethod]
    public void ResourceCulturesHaveMatchingKeys()
    {
        string root = FindRepositoryRoot();
        string neutralPath = Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Shared", "Localization", "Strings.resx");
        string traditionalChinesePath = Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Shared", "Localization", "Strings.zh-TW.resx");
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
        Regex forbidden = new("throw new [A-Za-z0-9_.<>]+Exception\\(\\s*\"|MessageBox\\.Show\\(\\s*\"|Console\\.Write(?:Line)?\\(\\s*\"|WindowsLogManager\\.Instance\\.WriteEntry\\(\\s*\"|GenericErrorDialog(?:\\s+[A-Za-z0-9_]+\\s*=)?\\s*new\\(\\s*\"|EventMessage\\s*=\\s*\"|SetToolTip\\([^,]+,\\s*(?:string\\.Format\\()?\\s*\"", RegexOptions.Compiled);
        Regex designerText = new("\\.(?:Text|HeaderText|ToolTipText|ToolTipTitle)\\s*=\\s*\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
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

        Assert.IsEmpty(violations.Distinct(StringComparer.OrdinalIgnoreCase), string.Join(Environment.NewLine, violations.Distinct()));
    }
    /// <summary>
    /// Verifies that literal keys passed to the localization API exist in both resource cultures.
    /// </summary>
    [TestMethod]
    public void LocalizedCallsReferenceExistingResources()
    {
        string root = FindRepositoryRoot();
        HashSet<string> keys = LoadResourceKeys(Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Shared", "Localization", "Strings.resx"));
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
        Assert.IsEmpty(missing, string.Join(Environment.NewLine, missing));
    }
    /// <summary>
    /// Verifies that the removed licensing feature is not exposed by application UI or command sources.
    /// </summary>
    [TestMethod]
    public void UserFacingSourcesDoNotExposeRemovedLicensingFeature()
    {
        string root = FindRepositoryRoot();
        string[] sourceRoots =
        [
            Path.Combine(root, "src", "IDDSCommunity.IntrusionDetection.Admin")
        ];
        Regex removedFeature = new("\\b(?:licen[cs](?:e|ing|ed)?|activation|pro edition|unlimited edition|register online)\\b|授權|僅限專業版|無限版本|線上註冊", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        List<string> violations = [];

        foreach (string sourceRoot in sourceRoots)
        {
            foreach (string file in Directory.EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
                         .Where(path => Path.GetExtension(path) is ".cs" or ".resx"
                                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
            {
                if (removedFeature.IsMatch(File.ReadAllText(file)))
                    violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }
    /// <summary>
    /// Loads resource keys from a resx file.
    /// </summary>
    /// <param name="path">The resx path.</param>
    /// <returns>傳回 unique resource keys 的結果。</returns>
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
        Assert.IsEmpty(duplicates, string.Join(Environment.NewLine, duplicates));
    }
    /// <summary>
    /// Finds the repository root from the test output directory.
    /// </summary>
    /// <returns>傳回 repository root path 的結果。</returns>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IDDSCommunity.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("IDDSCommunity repository root was not found.");
    }
}

using System.Globalization;
using Cyberarms.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}

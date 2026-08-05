using System;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// Verifies that report labels, dates, and mail subjects follow the configured application language.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ReportLocalizationTest
{
    private const string Template = "[%LABEL_INSTALLATION_INFORMATION%]|[%LABEL_EVENTS_PER_AGENT%]|[%LABEL_INTRUSION_ATTEMPTS%]|[%LABEL_REPORT_CONFIGURATION_HINT%]";

    /// <summary>
    /// Restores the invariant test culture after each global language-manager assertion.
    /// </summary>
    [TestCleanup]
    public void RestoreLanguage() => LanguageManager.Instance.Initialize("en");

    /// <summary>
    /// Verifies the invariant English report output.
    /// </summary>
    [TestMethod]
    public void LocalizeReportTemplate_English_UsesEnglishLabels()
    {
        LanguageManager.Instance.Initialize("en");

        string result = ReportGenerator.LocalizeReportTemplate(Template);

        StringAssert.Contains(result, "Installation information");
        StringAssert.Contains(result, "Events per agent");
        StringAssert.Contains(result, "Intrusion attempts");
        Assert.IsFalse(result.Contains("[%LABEL_", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies Traditional Chinese labels and culture-aware report subject formatting.
    /// </summary>
    [TestMethod]
    public void LocalizeReportTemplate_TraditionalChinese_UsesLocalizedLabelsAndSubject()
    {
        LanguageManager.Instance.Initialize("zh-TW");

        string result = ReportGenerator.LocalizeReportTemplate(Template);
        string subject = Strings.Format("Daily report for {0}", "server01");

        StringAssert.Contains(result, "安裝資訊");
        StringAssert.Contains(result, "各 Agent 事件");
        StringAssert.Contains(result, "入侵嘗試");
        Assert.AreEqual("server01 的每日報表", subject);
        Assert.IsFalse(result.Contains("[%LABEL_", StringComparison.Ordinal));
    }
}

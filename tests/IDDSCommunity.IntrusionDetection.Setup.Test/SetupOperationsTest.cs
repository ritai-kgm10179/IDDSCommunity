using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Setup.Test;

[TestClass]
[SupportedOSPlatform("windows7.0")]
public sealed class SetupOperationsTest
{
    [TestMethod]
    public void CompareVersions_NormalizesMissingComponents()
    {
        Assert.AreEqual(0, SetupOperations.CompareVersions(new Version(3, 0), new Version(3, 0, 0, 0)));
        Assert.IsTrue(SetupOperations.CompareVersions(new Version(3, 1), new Version(3, 0, 9)) > 0);
    }

    [TestMethod]
    public void CopyDirectoryOverwrite_CopiesCompletePayload()
    {
        using TemporaryDirectory source = new();
        using TemporaryDirectory destination = new();
        string nested = Path.Combine(source.Path, "agents");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "agent.dll"), "payload");

        SetupOperations.CopyDirectoryOverwrite(source.Path, destination.Path, CancellationToken.None);

        Assert.AreEqual("payload", File.ReadAllText(Path.Combine(destination.Path, "agents", "agent.dll")));
    }

    [TestMethod]
    public void CopyDirectoryOverwrite_HonorsCancellationBeforeChangingDestination()
    {
        using TemporaryDirectory source = new();
        using TemporaryDirectory destination = new();
        File.WriteAllText(Path.Combine(source.Path, "payload.bin"), "payload");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            SetupOperations.CopyDirectoryOverwrite(source.Path, destination.Path, cancellation.Token));
        Assert.IsFalse(File.Exists(Path.Combine(destination.Path, "payload.bin")));
    }

    [TestMethod]
    public void SafeDeleteDirectory_DeletesRebuildableTreeWithoutRestart()
    {
        TemporaryDirectory directory = new();
        string path = directory.Path;
        File.WriteAllText(Path.Combine(path, "temporary.bin"), "temporary");

        bool restartRequired = SetupOperations.SafeDeleteDirectory(path);

        Assert.IsFalse(restartRequired);
        Assert.IsFalse(Directory.Exists(path));
        directory.SuppressCleanup();
    }

    [TestMethod]
    public void SetupText_ContainsAllOperationalMessagesInBothLanguages()
    {
        string[] keys =
        [
            "ProgressPreparing", "ProgressValidating", "ProgressStoppingService", "ProgressInstallingFiles",
            "ProgressRegisteringService", "ProgressStartingService", "ProgressRemovingFiles", "ProgressCompleted",
            "ProgressCancelling", "CancelledAndRolledBack", "RestartRequired", "OperationFailed",
            "DiagnosticLogPath", "RollbackFailed", "RollbackServiceMissing", "DeleteFailed",
            "ShortcutCreationFailed", "FirewallCleanupStartFailed", "FirewallCleanupTimedOut",
            "FirewallCleanupFailed", "ServiceControlTimedOut", "ServiceControlFailedWithDetails",
            "ApplicationLaunchFailed", "ProcessStopTimedOut", "CleanupIncomplete", "TransactionAlreadyCommitted",
            "ServiceStopVerificationFailed", "ServiceStateStabilizationFailed", "ServicePauseVerificationFailed"
        ];
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            foreach (string cultureName in new[] { "en-US", "zh-TW" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                foreach (string key in keys)
                    Assert.AreNotEqual(key, SetupText.Get(key), $"Missing {key} in {cultureName}.");
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void SetupText_UsesTaiwanOfficialLanguageName()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");

            Assert.AreEqual("🌐 正體中文", SetupText.Get("LanguageButtonText"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [STATestMethod]
    public void SetupForm_LocalizedControlsRemainInsideTheirContainersAtSupportedScaleFactors()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            foreach (string cultureName in new[] { "en-US", "zh-TW" })
            {
                foreach (float scaleFactor in new[] { 1F, 1.25F, 1.5F, 2F })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                    using SetupForm form = new();
                    form.Size = form.MinimumSize;
                    form.CreateControl();
                    if (scaleFactor != 1F) form.Scale(new System.Drawing.SizeF(scaleFactor, scaleFactor));
                    form.PerformLayout();
                    foreach (Control control in EnumerateControls(form))
                    {
                        if (!control.Visible || control.Parent is null) continue;
                        Assert.IsTrue(control.Left >= 0 && control.Right <= control.Parent.ClientSize.Width,
                            $"{cultureName} at {scaleFactor:P0}: {control.GetType().Name} is horizontally clipped.");
                    }
                }
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [TestMethod]
    public void SetupRollbackJournal_RollsBackInReverseOrder()
    {
        List<int> order = [];
        SetupRollbackJournal journal = new();
        journal.Record(() => order.Add(1));
        journal.Record(() => order.Add(2));
        journal.Record(() => order.Add(3));

        journal.RollBack();

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, order);
    }

    [TestMethod]
    public void SetupRollbackJournal_ContinuesAfterRollbackFailure()
    {
        List<int> order = [];
        SetupRollbackJournal journal = new();
        journal.Record(() => order.Add(1));
        journal.Record(() => throw new IOException("Injected rollback failure."));
        journal.Record(() => order.Add(3));

        AggregateException failure = Assert.ThrowsExactly<AggregateException>(journal.RollBack);

        Assert.HasCount(1, failure.InnerExceptions);
        CollectionAssert.AreEqual(new[] { 3, 1 }, order);
    }

    [TestMethod]
    public void SetupRollbackJournal_DoesNothingAfterCommit()
    {
        bool rolledBack = false;
        SetupRollbackJournal journal = new();
        journal.Record(() => rolledBack = true);
        journal.Commit();

        journal.RollBack();

        Assert.IsFalse(rolledBack);
    }

    [TestMethod]
    [DataRow(ServiceControllerStatus.StartPending, ServiceControllerStatus.Running)]
    [DataRow(ServiceControllerStatus.ContinuePending, ServiceControllerStatus.Running)]
    [DataRow(ServiceControllerStatus.StopPending, ServiceControllerStatus.Stopped)]
    [DataRow(ServiceControllerStatus.PausePending, ServiceControllerStatus.Paused)]
    [DataRow(ServiceControllerStatus.Running, ServiceControllerStatus.Running)]
    [DataRow(ServiceControllerStatus.Stopped, ServiceControllerStatus.Stopped)]
    [DataRow(ServiceControllerStatus.Paused, ServiceControllerStatus.Paused)]
    public void GetStableServiceStatusTarget_MapsEveryWindowsServiceState(
        ServiceControllerStatus current,
        ServiceControllerStatus expected)
    {
        Assert.AreEqual(expected, SetupOperations.GetStableServiceStatusTarget(current));
    }

    private static IEnumerable<Control> EnumerateControls(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in EnumerateControls(child)) yield return descendant;
        }
    }

    [TestMethod]
    public void CreateDataDirectorySecurity_WithoutOperatorsSid_DeniesUnauthorizedPrincipalsAndGrantsSystemAndAdmins()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("NTFS ACL security descriptor verification requires Windows.");

        DirectorySecurity security = SetupOperations.CreateDataDirectorySecurity(operatorsSid: null);

        Assert.IsTrue(security.AreAccessRulesProtected, "資料目錄安全描述元必須停用繼承。");

        SecurityIdentifier builtinUsersSid = new(WellKnownSidType.BuiltinUsersSid, null);
        SecurityIdentifier worldSid = new(WellKnownSidType.WorldSid, null);
        SecurityIdentifier authenticatedUserSid = new(WellKnownSidType.AuthenticatedUserSid, null);
        SecurityIdentifier localSystemSid = new(WellKnownSidType.LocalSystemSid, null);
        SecurityIdentifier builtinAdminsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);

        AuthorizationRuleCollection rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier));
        Assert.IsTrue(rules.Count >= 2, "安全描述元至少必須包含 SYSTEM 與 Administrators 規則。");

        bool hasSystem = false;
        bool hasAdmins = false;

        foreach (FileSystemAccessRule rule in rules)
        {
            Assert.AreNotEqual(builtinUsersSid, rule.IdentityReference, "BUILTIN\\Users 不得出現在資料目錄安全描述元中。");
            Assert.AreNotEqual(worldSid, rule.IdentityReference, "Everyone 不得出現在資料目錄安全描述元中。");
            Assert.AreNotEqual(authenticatedUserSid, rule.IdentityReference, "Authenticated Users 不得出現在資料目錄安全描述元中（違反 AGENTS.md 規範第 8 條）。");

            if (rule.IdentityReference.Equals(localSystemSid))
            {
                hasSystem = true;
                Assert.AreEqual(FileSystemRights.FullControl, rule.FileSystemRights, "SYSTEM 必須具有完全控制。");
                Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
            }

            if (rule.IdentityReference.Equals(builtinAdminsSid))
            {
                hasAdmins = true;
                Assert.AreEqual(FileSystemRights.FullControl, rule.FileSystemRights, "Administrators 必須具有完全控制。");
                Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
            }
        }

        Assert.IsTrue(hasSystem, "SYSTEM 必須擁有 FullControl 存取。");
        Assert.IsTrue(hasAdmins, "BuiltinAdministrators 必須擁有 FullControl 存取。");
    }

    [TestMethod]
    public void CreateDataDirectorySecurity_WithOperatorsSid_IncludesModifyRuleForOperators()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("NTFS ACL security descriptor verification requires Windows.");

        // 使用目前使用者的 SID 模擬操作人員群組 SID（CI 環境無法保證群組存在）
        SecurityIdentifier? operatorsSid = System.Security.Principal.WindowsIdentity.GetCurrent().User;
        Assert.IsNotNull(operatorsSid, "無法取得目前使用者 SID 作為測試替代。");

        DirectorySecurity security = SetupOperations.CreateDataDirectorySecurity(operatorsSid);

        AuthorizationRuleCollection rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier));

        bool hasOperatorsModify = false;
        foreach (FileSystemAccessRule rule in rules)
        {
            if (rule.IdentityReference.Equals(operatorsSid) && rule.AccessControlType == AccessControlType.Allow)
            {
                // Modify 是一組複合旗標；確認至少包含 WriteData（寫入）與 ReadData（讀取）
                Assert.IsTrue(
                    rule.FileSystemRights.HasFlag(FileSystemRights.WriteData) &&
                    rule.FileSystemRights.HasFlag(FileSystemRights.ReadData),
                    "操作人員規則必須至少包含 ReadData 與 WriteData（Modify 的子集）。");
                hasOperatorsModify = true;
            }
        }

        Assert.IsTrue(hasOperatorsModify, "操作人員 SID 必須在安全描述元中出現並具有 Modify（含）以上的存取權限。");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private bool cleanup = true;

        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "idds-setup-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal void SuppressCleanup() => cleanup = false;

        public void Dispose()
        {
            if (cleanup && Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}

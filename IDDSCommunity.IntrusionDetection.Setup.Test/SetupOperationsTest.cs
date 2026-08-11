using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;
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
            "ApplicationLaunchFailed", "ProcessStopTimedOut", "CleanupIncomplete"
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

    [STATestMethod]
    public void SetupForm_LocalizedControlsRemainInsideTheirContainers()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            foreach (string cultureName in new[] { "en-US", "zh-TW" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                using SetupForm form = new();
                form.Size = form.MinimumSize;
                form.CreateControl();
                form.PerformLayout();
                foreach (Control control in EnumerateControls(form))
                {
                    if (!control.Visible || control.Parent is null) continue;
                    Assert.IsTrue(control.Left >= 0 && control.Right <= control.Parent.ClientSize.Width,
                        $"{cultureName}: {control.GetType().Name} is horizontally clipped.");
                }
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    private static IEnumerable<Control> EnumerateControls(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in EnumerateControls(child)) yield return descendant;
        }
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

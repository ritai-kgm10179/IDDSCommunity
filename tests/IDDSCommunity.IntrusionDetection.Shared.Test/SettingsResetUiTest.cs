using System.Collections.Generic;
using System.Drawing;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 Admin 設定頁的恢復預設值互動不會略過儲存與取消流程。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SettingsResetUiTest
{
    [TestInitialize]
    public void Init()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }
    /// <summary>
    /// 驗證程式化同步選取 Agent 時不會再次引發導覽事件。
    /// </summary>
    [STATestMethod]
    public void SettingsNavigation_SilentSelection_DoesNotRaiseNavigationChanged()
    {
        using IDDSCommunitySettingsNavigation navigation = new();
        navigation.AddNavigationItem("first", null, null);
        navigation.AddNavigationItem("second", null, null);
        int notifications = 0;
        navigation.NavigationChanged += (_, _) => notifications++;

        navigation.SetSelectedItem("second", notify: false);

        Assert.AreEqual("second", navigation.SelectedName);
        Assert.AreEqual(0, notifications);
    }

    /// <summary>
    /// 驗證重複選取目前項目時不會再次引發導覽事件。
    /// </summary>
    [STATestMethod]
    public void SettingsNavigation_SelectCurrentItem_DoesNotRaiseDuplicateEvent()
    {
        using IDDSCommunitySettingsNavigation navigation = new();
        navigation.AddNavigationItem("first", null, null);
        int notifications = 0;
        navigation.NavigationChanged += (_, _) => notifications++;

        navigation.SetSelectedItem("first");
        navigation.SetSelectedItem("first");

        Assert.AreEqual(0, notifications);
    }

    /// <summary>
    /// 驗證大量程式化切換只在明確要求通知時引發一次事件。
    /// </summary>
    [STATestMethod]
    public void SettingsNavigation_RapidProgrammaticSelections_DoNotCreateEventStorm()
    {
        using IDDSCommunitySettingsNavigation navigation = new();
        navigation.AddNavigationItem("first", null, null);
        navigation.AddNavigationItem("second", null, null);
        int notifications = 0;
        navigation.NavigationChanged += (_, _) => notifications++;

        for (int index = 0; index < 1_000; index++)
            navigation.SetSelectedItem(index % 2 == 0 ? "second" : "first", notify: false);
        navigation.SetSelectedItem("second");

        Assert.AreEqual("second", navigation.SelectedName);
        Assert.AreEqual(1, notifications);
    }

    /// <summary>
    /// 驗證停止服務操作時會等待既有操作離開，且拒絕後續操作。
    /// </summary>
    [TestMethod]
    public void OperationDrain_Stop_DefersCleanupUntilLastOperationCompletes()
    {
        OperationDrain drain = new();
        Assert.IsTrue(drain.TryEnter(out IDisposable? first));
        Assert.IsTrue(drain.TryEnter(out IDisposable? second));
        int cleanupCalls = 0;

        drain.Stop(() => Interlocked.Increment(ref cleanupCalls));

        Assert.IsFalse(drain.TryEnter(out _));
        first!.Dispose();
        Assert.AreEqual(0, cleanupCalls);
        second!.Dispose();
        Assert.AreEqual(1, cleanupCalls);
        second.Dispose();
        Assert.AreEqual(1, cleanupCalls);
    }

    /// <summary>
    /// 驗證沒有執行中操作時，停止要求會立即且只清理一次。
    /// </summary>
    [TestMethod]
    public void OperationDrain_StopWhenIdle_CleansUpOnce()
    {
        OperationDrain drain = new();
        int cleanupCalls = 0;

        drain.Stop(() => Interlocked.Increment(ref cleanupCalls));
        drain.Stop(() => Interlocked.Increment(ref cleanupCalls));

        Assert.AreEqual(1, cleanupCalls);
        Assert.IsFalse(drain.TryEnter(out _));
    }

    /// <summary>
    /// 驗證沒有待存設定時，第一次關閉不會被非同步流程取消。
    /// </summary>
    [STATestMethod]
    public void IddsAdmin_CloseWithoutPendingAgentSave_AllowsFirstClose()
    {
        using IddsAdmin admin = new();
        FormClosingEventArgs closing = new(CloseReason.UserClosing, cancel: false);

        InvokeFormClosing(admin, closing);

        Assert.IsFalse(closing.Cancel);
        CancellationTokenSource cancellation = Assert.IsInstanceOfType<CancellationTokenSource>(
            typeof(IddsAdmin).GetField("uiRefreshCancellation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(admin));
        Assert.IsTrue(cancellation.IsCancellationRequested);
    }

    /// <summary>
    /// 驗證待存設定期間只取消關閉一次，Dispose 會取消持久化 continuation。
    /// </summary>
    [TestMethod]
    public async Task IddsAdmin_DisposeDuringPendingAgentSave_CancelsContinuationSafely()
    {
        TaskCompletionSource saveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource saveCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PanelPluginConfiguration pluginPanel = new(null, async (snapshot, cancellationToken) =>
        {
            saveStarted.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return new AgentSettingsSaveResult(snapshot, snapshot.Id, snapshot.Serial);
            }
            catch (OperationCanceledException)
            {
                saveCanceled.SetResult();
                throw;
            }
        }, (_, _, _) => { })
        {
            Agent = new SecurityAgent { Name = "test", DisplayName = "test", HardLockAttempts = 5 }
        };
        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(pluginPanel.Controls.Find("textBoxHardLocks", true)[0]);
        hardLocks.Text = "6";

        IDDSCommunityAgentConfiguration agentConfiguration = new();
        typeof(IDDSCommunityAgentConfiguration).GetField("_pluginConfigPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(agentConfiguration, pluginPanel);
        agentConfiguration.Controls.Add(pluginPanel);

        IddsAdmin admin = new();
        typeof(IddsAdmin).GetField("_panelAgentConfiguration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(admin, agentConfiguration);
        admin.Controls.Add(agentConfiguration);

        FormClosingEventArgs firstClosing = new(CloseReason.UserClosing, cancel: false);
        InvokeFormClosing(admin, firstClosing);
        await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        FormClosingEventArgs secondClosing = new(CloseReason.UserClosing, cancel: false);
        InvokeFormClosing(admin, secondClosing);

        Assert.IsTrue(firstClosing.Cancel);
        Assert.IsTrue(secondClosing.Cancel);
        admin.Dispose();
        await saveCanceled.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        Assert.IsTrue(admin.IsDisposed);
    }

    private static void InvokeFormClosing(IddsAdmin admin, FormClosingEventArgs closing) =>
        typeof(IddsAdmin).GetMethod("OnFormClosing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(admin, [closing]);

    /// <summary>
    /// 驗證操作執行期間的多次重新要求只會合併成一次後續執行。
    /// </summary>
    [TestMethod]
    public async Task CoalescingAsyncOperation_BurstDuringRun_ExecutesOneFollowUp()
    {
        TaskCompletionSource firstRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        CoalescingAsyncOperation operation = new(async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                await firstRelease.Task;
        });

        Task first = operation.RequestAsync();
        Task second = operation.RequestAsync();
        Task third = operation.RequestAsync();
        firstRelease.SetResult();
        await Task.WhenAll(first, second, third);

        Assert.AreEqual(2, calls);
    }

    /// <summary>
    /// 驗證操作失敗會傳回呼叫端，且協調器之後仍可接受新的要求。
    /// </summary>
    [TestMethod]
    public async Task CoalescingAsyncOperation_FailureIsVisible_AndNextRequestCanRun()
    {
        int calls = 0;
        CoalescingAsyncOperation operation = new(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                return Task.FromException(new InvalidOperationException("visible"));
            return Task.CompletedTask;
        });

        InvalidOperationException failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => operation.RequestAsync());
        Assert.AreEqual("visible", failure.Message);
        await operation.RequestAsync();
        Assert.AreEqual(2, calls);
    }

    /// <summary>
    /// 驗證背景 Agent 儲存等待期間 STA 訊息迴圈仍可處理 Windows 訊息。
    /// </summary>
    [STATestMethod]
    public async Task AgentSaveQueue_DelayedPersistence_KeepsStaMessagePumpResponsive()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SecurityAgent agent = new() { Name = "test", DisplayName = "test" };
        AgentSettingsSnapshot snapshot = new(
            agent, 1, System.Guid.NewGuid(), "test", string.Empty, "test", 0,
            5, 24, false, 3, 10, false, true,
            AgentSettingsSnapshot.Freeze(new Dictionary<string, string>()));
        AgentSettingsSaveQueue queue = new(async (value, cancellationToken) =>
        {
            await release.Task.WaitAsync(cancellationToken);
            return new AgentSettingsSaveResult(value, value.Id, value.Serial + 1);
        });
        bool messageProcessed = false;
        using System.Windows.Forms.Timer timer = new() { Interval = 10 };
        timer.Tick += (_, _) => messageProcessed = true;
        timer.Start();

        Task<AgentSettingsSaveResult> pending = queue.EnqueueAsync(snapshot, CancellationToken.None);
        System.Diagnostics.Stopwatch timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!messageProcessed && timeout.Elapsed < System.TimeSpan.FromSeconds(2))
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }

        Assert.IsTrue(messageProcessed, "背景儲存等待期間 STA 訊息迴圈應持續處理計時器訊息。");
        Assert.IsFalse(pending.IsCompleted);
        release.SetResult();
        await pending;
    }

    /// <summary>
    /// 驗證 SQLite busy 失敗後設定仍維持待存狀態，後續重試會再次持久化。
    /// </summary>
    [STATestMethod]
    public async Task PanelPluginConfiguration_BusySaveFailure_RemainsDirtyForRetry()
    {
        int attempts = 0;
        using PanelPluginConfiguration panel = new(null, (_, _) =>
        {
            Interlocked.Increment(ref attempts);
            return Task.FromException<AgentSettingsSaveResult>(new SqliteException("busy", 5));
        }, (_, _, _) => { })
        {
            Agent = new SecurityAgent { Name = "test", DisplayName = "test", HardLockAttempts = 5 }
        };
        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLocks", true)[0]);
        hardLocks.Text = "6";

        Assert.IsFalse(await panel.FlushUnsavedChangesAsync());
        Assert.IsFalse(await panel.FlushUnsavedChangesAsync());
        Assert.AreEqual(2, attempts);
    }

    /// <summary>
    /// 驗證 Agent 的恢復預設值操作只更新待儲存畫面，不會立即改寫執行中設定。
    /// </summary>
    [STATestMethod]
    public void AgentResetButton_StagesDefaultsWithoutImmediatePersistence()
    {
        SecurityAgent agent = new()
        {
            Enabled = true,
            OverrideConfig = true,
            LockForever = true,
            HardLockAttempts = 99,
            HardLockTimeHours = 48,
            SoftLockAttempts = 88,
            SoftLockTimeMinutes = 44,
            DefaultCustomConfiguration = new Dictionary<string, string> { ["Port"] = "25" },
            CustomConfiguration = new Dictionary<string, string> { ["Port"] = "2525" },
            CustomConfigurationTypes = new Dictionary<string, string> { ["Port"] = typeof(int).FullName! }
        };
        using PanelPluginConfiguration panel = new(_ => DialogResult.Yes) { Agent = agent };

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        reset.PerformClick();

        Assert.AreEqual(99, agent.HardLockAttempts);
        Assert.IsTrue(agent.OverrideConfig);
        Assert.IsTrue(agent.Enabled);
        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLocks", true)[0]);
        CheckBox enabled = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableSecurityAgent", true)[0]);
        CheckBox overwrite = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxOverrideConfiguration", true)[0]);
        Button save = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonSave", true)[0]);
        Assert.AreEqual(IddsConfig.DefaultHardLockAttempts.ToString(), hardLocks.Text);
        Assert.IsTrue(enabled.Checked);
        Assert.IsFalse(overwrite.Checked);
        Assert.IsTrue(save.Visible);
        Assert.HasCount(0, panel.Controls.Find("buttonDiscard", true));
    }

    /// <summary>
    /// 驗證使用者取消確認提示時不會變更待編輯設定。
    /// </summary>
    [STATestMethod]
    public void AgentResetButton_CancelledConfirmationLeavesSettingsUnchanged()
    {
        SecurityAgent agent = new()
        {
            HardLockAttempts = 99,
            DefaultCustomConfiguration = new Dictionary<string, string> { ["Port"] = "25" },
            CustomConfiguration = new Dictionary<string, string> { ["Port"] = "2525" },
            CustomConfigurationTypes = new Dictionary<string, string> { ["Port"] = typeof(int).FullName! }
        };
        using PanelPluginConfiguration panel = new(_ => DialogResult.No) { Agent = agent };

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        reset.PerformClick();

        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLocks", true)[0]);
        Button save = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonSave", true)[0]);
        Assert.AreEqual("99", hardLocks.Text);
        Assert.IsTrue(save.Visible);
    }

    /// <summary>
    /// 驗證 Agent 啟用狀態變更後，Dashboard 可立即替換狀態圖示而不必等待定時更新。
    /// </summary>
    [STATestMethod]
    public void DashboardRefreshAgentPresentations_ImmediatelyUpdatesEnabledStateIcon()
    {
        SecurityAgent agent = new()
        {
            DisplayName = "Test Agent",
            Enabled = false
        };
        using IDDSCommunityDashboard dashboard = new();
        dashboard.AddAgent(agent);
        PictureBox status = Assert.IsInstanceOfType<PictureBox>(dashboard.Controls.Find("pictureBoxEnabledState", true)[0]);
        Assert.IsNotNull(status.Image);
        Image disabledImage = status.Image;
        string disabledAccessibleName = status.AccessibleName ?? string.Empty;

        agent.Enabled = true;
        dashboard.RefreshAgentPresentations();

        Assert.IsNotNull(status.Image);
        Image enabledImage = status.Image;
        Assert.AreNotSame(disabledImage, enabledImage);
        Assert.AreNotEqual(disabledAccessibleName, status.AccessibleName);
    }

    /// <summary>
    /// 驗證共用按鈕在較窄的可視容器內仍保持完整可見。
    /// </summary>
    [STATestMethod]
    public void ResetButton_RemainsInsideVisibleParentBounds()
    {
        using Panel viewport = new() { ClientSize = new System.Drawing.Size(500, 500) };
        using PanelNotificationSettings settings = new() { Size = new System.Drawing.Size(620, 400) };
        viewport.Controls.Add(settings);
        viewport.PerformLayout();
        settings.PerformLayout();

        Button reset = Assert.IsInstanceOfType<Button>(settings.Controls.Find("buttonResetDefaults", true)[0]);
        Assert.IsTrue(reset.Left >= 0);
        Assert.IsTrue(reset.Right <= viewport.ClientSize.Width);

        viewport.ClientSize = new System.Drawing.Size(420, 500);
        viewport.PerformLayout();
        settings.PerformLayout();
        Assert.IsTrue(reset.Right <= viewport.ClientSize.Width);
    }

    /// <summary>
    /// 驗證長代理程式名稱於中英文介面下自動折行，且標題區域絕不與恢復預設值按鈕重疊。
    /// </summary>
    /// <param name="agentDisplayName">待測試之代理程式顯示名稱。</param>
    [STATestMethod]
    [DataRow("Windows 遠端管理 ( WinRM / WAC ) 安全性代理程式")]
    [DataRow("Windows Remote Management (WinRM / WAC) Security Agent")]
    public void PanelPluginConfiguration_LongAgentTitle_WrapsAndDoesNotOverlapWithResetButton(string agentDisplayName)
    {
        SecurityAgent agent = new()
        {
            DisplayName = agentDisplayName
        };
        using Panel viewport = new() { ClientSize = new System.Drawing.Size(480, 500) };
        using PanelPluginConfiguration panel = new(_ => DialogResult.No)
        {
            Size = new System.Drawing.Size(480, 500),
            Agent = agent
        };
        viewport.Controls.Add(panel);
        viewport.PerformLayout();
        panel.PerformLayout();

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        SmartLabel title = Assert.IsInstanceOfType<SmartLabel>(panel.Controls.Find("smartLabelAgentName", true)[0]);

        Assert.IsFalse(title.Bounds.IntersectsWith(reset.Bounds),
            $"標題邊界 ({title.Bounds}) 不得與恢復預設值按鈕邊界 ({reset.Bounds}) 重疊。");
        Assert.IsTrue(title.Right <= reset.Left,
            $"標題右邊緣 ({title.Right}) 必須位於恢復預設值按鈕左邊緣 ({reset.Left}) 之前。");
    }
}

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

internal sealed record AgentSettingsSnapshot(
    SecurityAgent Target,
    long EditGeneration,
    Guid Id,
    string Name,
    string AssemblyName,
    string DisplayName,
    int Serial,
    int HardLockAttempts,
    int HardLockTimeHours,
    bool LockForever,
    int SoftLockAttempts,
    int SoftLockTimeMinutes,
    bool OverrideConfig,
    bool Enabled,
    IReadOnlyDictionary<string, string> CustomConfiguration)
{
    internal SecurityAgent CreatePersistenceAgent()
    {
        return new SecurityAgent
        {
            DatabaseInstance = Target.DatabaseInstance,
            Id = Id,
            Name = Name,
            AssemblyName = AssemblyName,
            DisplayName = DisplayName,
            Serial = Serial,
            HardLockAttempts = HardLockAttempts,
            HardLockTimeHours = HardLockTimeHours,
            LockForever = LockForever,
            SoftLockAttempts = SoftLockAttempts,
            SoftLockTimeMinutes = SoftLockTimeMinutes,
            OverrideConfig = OverrideConfig,
            Enabled = Enabled,
            CustomConfiguration = new Dictionary<string, string>(CustomConfiguration, StringComparer.Ordinal),
            CustomConfigurationTypes = new Dictionary<string, string>(Target.CustomConfigurationTypes, StringComparer.Ordinal)
        };
    }

    internal static IReadOnlyDictionary<string, string> Freeze(IDictionary<string, string> values) =>
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values, StringComparer.Ordinal));
}

internal sealed record AgentSettingsSaveResult(AgentSettingsSnapshot Snapshot, Guid PersistedId, int PersistedSerial);

internal sealed class AgentSettingsSaveQueue
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Func<AgentSettingsSnapshot, CancellationToken, Task<AgentSettingsSaveResult>> saveOperation;

    internal AgentSettingsSaveQueue(Func<AgentSettingsSnapshot, CancellationToken, Task<AgentSettingsSaveResult>> saveOperation)
    {
        this.saveOperation = saveOperation ?? throw new ArgumentNullException(nameof(saveOperation));
    }

    internal async Task<AgentSettingsSaveResult> EnqueueAsync(AgentSettingsSnapshot snapshot, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await saveOperation(snapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }
}

/// <summary>
/// 提供擴充元件目錄與外掛模組清單設定之面板控制項。
/// </summary>
public partial class PanelPluginConfiguration : UserControl
{
        /// <summary>
    /// 當 AgentChanged 時引發之事件。
    /// </summary>
public event EventHandler? AgentChanged;
        /// <summary>
    /// 當 AgentConfigurationChanged 時引發之事件。
    /// </summary>
public event EventHandler? AgentConfigurationChanged;
    /// <summary>
    /// 初始化 <see cref="PanelPluginConfiguration"/> 類別的新執行個體。
    /// </summary>
    public PanelPluginConfiguration()
        : this(null, null)
    {
    }

    private readonly Button _buttonResetDefaults;
    private readonly Action<IWin32Window, string, MessageBoxIcon>? saveErrorPresenter;
    private bool _isUpdatingHeaderLayout;

    /// <summary>
    /// 初始化可指定恢復預設值確認提示的設定面板。
    /// </summary>
    /// <param name="confirmationPrompt">確認提示委派；正式執行時傳入 <see langword="null"/>。</param>
    internal PanelPluginConfiguration(Func<IWin32Window, DialogResult>? confirmationPrompt)
        : this(confirmationPrompt, null)
    {
    }

    internal PanelPluginConfiguration(
        Func<IWin32Window, DialogResult>? confirmationPrompt,
        Func<AgentSettingsSnapshot, CancellationToken, Task<AgentSettingsSaveResult>>? saveOperation,
        Action<IWin32Window, string, MessageBoxIcon>? saveErrorPresenter = null)
    {
        this.saveErrorPresenter = saveErrorPresenter;
        _saveQueue = new AgentSettingsSaveQueue(saveOperation ?? PersistSnapshotAsync);
        InitializeComponent();
        textBoxHardLocks.TextChanged += textBox_TextChanged;
        textBoxHardLockDuration.TextChanged += textBox_TextChanged;
        textBoxSoftLocks.TextChanged += textBox_TextChanged;
        textBoxSoftLockDuration.TextChanged += textBox_TextChanged;
        flowLayoutPanelCustomPluginSettings.ClientSizeChanged += (_, _) => UpdateCustomSettingsLayout();
        AgentChanged += new EventHandler(PanelPluginConfiguration_AgentChanged);
        _buttonResetDefaults = SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click, confirmationPrompt: confirmationPrompt, container: headerPanel);
        headerPanel.ClientSizeChanged += (_, _) => UpdateHeaderLayout();
        headerPanel.Layout += (_, _) => UpdateHeaderLayout();
        UpdateHeaderLayout();
        Disposed += (_, _) => _lifetimeCancellation.Cancel();
    }
    /// <summary>
    /// 處理 agent changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void PanelPluginConfiguration_AgentChanged(object? sender, EventArgs e)
    {
        LoadData();
        smartLabelAgentName.Text = Agent.DisplayName;
        UpdateHeaderLayout();
        ClearErrors();
        AutoScrollPosition = Point.Empty;
        if (Parent is System.Windows.Forms.ScrollableControl scrollableParent)
        {
            scrollableParent.AutoScrollPosition = Point.Empty;
        }
    }
    /// <summary>
    /// Loads data.
    /// </summary>
    private bool _isLoadingData;
    /// <summary>
    /// Loads data.
    /// </summary>
    private void LoadData()
    {
        _isLoadingData = true;
        try
        {
            checkBoxLockForever.Checked = Agent.LockForever;
            textBoxHardLocks.Text = Agent.HardLockAttempts.ToString();
            textBoxHardLockDuration.Text = Agent.HardLockTimeHours.ToString();
            textBoxSoftLocks.Text = Agent.SoftLockAttempts.ToString();
            textBoxSoftLockDuration.Text = Agent.SoftLockTimeMinutes.ToString();
            checkBoxEnableSecurityAgent.Checked = Agent.Enabled;
            checkBoxOverrideConfiguration.Checked = Agent.OverrideConfig;
            SetEnabledMode(checkBoxOverrideConfiguration.Checked);
            LoadCustomSettings();
            smartLabelCustomConfig.Visible = flowLayoutPanelCustomPluginSettings.Controls.Count > 0;
            SetEditMode(false);
        }
        finally
        {
            _isLoadingData = false;
        }
    }
    /// <summary>
    /// Loads custom settings.
    /// </summary>
    private void LoadCustomSettings(IReadOnlyDictionary<string, string>? values = null)
    {
        while (flowLayoutPanelCustomPluginSettings.Controls.Count > 0)
        {
            Control child = flowLayoutPanelCustomPluginSettings.Controls[0];
            flowLayoutPanelCustomPluginSettings.Controls.RemoveAt(0);
            child.Dispose();
        }
        string? protectionDetails = GetProtectionDetails(Agent.Name);
        if (protectionDetails is not null)
        {
            int initialWidth = Math.Max(260, flowLayoutPanelCustomPluginSettings.ClientSize.Width
                - flowLayoutPanelCustomPluginSettings.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
            Label details = new()
            {
                AutoSize = true,
                MaximumSize = new Size(initialWidth, 0),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(102, 102, 102),
                Margin = new Padding(0, 2, 0, 8),
                Text = protectionDetails
            };
            flowLayoutPanelCustomPluginSettings.Controls.Add(details);
        }
        IReadOnlyDictionary<string, string> settings = values ?? Agent.CustomConfiguration;
        foreach (string propName in settings.Keys)
        {
            string propertyType = Agent.CustomConfigurationTypes.TryGetValue(propName, out string? declaredType)
                ? declaredType
                : typeof(string).FullName!;
            PluginSettingEditor editor = new(propName, propertyType, settings[propName], Agent.Name);
            editor.ValueChanged += (_, _) => SetEditMode(true);
            flowLayoutPanelCustomPluginSettings.Controls.Add(editor);
        }
        flowLayoutPanelCustomPluginSettings.Controls.Add(new Panel
        {
            Height = 16,
            Margin = Padding.Empty,
            TabStop = false
        });
        UpdateCustomSettingsLayout();
    }
    /// <summary>
    /// Keeps custom setting rows inside the visible client area at all DPI scaling levels.
    /// </summary>
    private void UpdateCustomSettingsLayout()
    {
        int availableWidth = Math.Max(260, flowLayoutPanelCustomPluginSettings.ClientSize.Width
            - flowLayoutPanelCustomPluginSettings.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
        foreach (Control control in flowLayoutPanelCustomPluginSettings.Controls)
        {
            if (control is Label label)
            {
                label.MaximumSize = new Size(availableWidth, 0);
            }
            control.Width = availableWidth;
        }
    }

    /// <summary>
    /// 動態計算標題寬度上限與容器高度，避免長代理程式名稱與恢復預設值按鈕重疊。
    /// </summary>
    private void UpdateHeaderLayout()
    {
        if (_isUpdatingHeaderLayout || headerPanel is null || smartLabelAgentName is null) return;
        _isUpdatingHeaderLayout = true;
        try
        {
            int buttonLeft = _buttonResetDefaults is not null && _buttonResetDefaults.Left > 0
                ? _buttonResetDefaults.Left
                : (headerPanel.ClientSize.Width - 120 - 20);

            int maxTitleWidth = Math.Max(80, buttonLeft - smartLabelAgentName.Left - 12);
            if (smartLabelAgentName.MaximumSize.Width != maxTitleWidth)
            {
                smartLabelAgentName.MaximumSize = new Size(maxTitleWidth, 0);
            }

            Size preferred = smartLabelAgentName.GetPreferredSize(new Size(maxTitleWidth, 0));
            int buttonHeight = _buttonResetDefaults?.Height ?? 32;
            int requiredHeight = Math.Max(buttonHeight + 4, smartLabelAgentName.Top + preferred.Height + 4);
            if (headerPanel.Height != requiredHeight)
            {
                headerPanel.Height = requiredHeight;
            }
            if (_buttonResetDefaults is not null)
            {
                _buttonResetDefaults.Top = Math.Max(0, (headerPanel.ClientSize.Height - _buttonResetDefaults.Height) / 2);
            }
        }
        finally
        {
            _isUpdatingHeaderLayout = false;
        }
    }
    /// <summary>
    /// Gets a localized description of the authoritative detection source and encrypted-traffic limitations.
    /// </summary>
    /// <param name="agentName">The fully qualified Agent type name.</param>
    /// <returns>在地化詳細資訊；若不需要則傳回 <see langword="null"/>。</returns>
    private static string? GetProtectionDetails(string agentName) => agentName switch
    {
        "IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher" => Strings.Get("Detection source: Windows Application Event Log, Event ID 18456. The database port is not scanned."),
        "IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher" => Strings.Get("Detection source: MySQL or MariaDB entries in the Windows Application Event Log. The database port is not scanned."),
        "IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent" => Strings.Get("Detection source: Windows Application Event Log, Event ID 661. The database port is not scanned."),
        "IDDSCommunity.Agents.MailServer.ImapAgent" => Strings.Get("Inspects cleartext IMAP on the configured port. Parsing stops after STARTTLS; implicit TLS on port 993 requires server-side logs."),
        _ => null
    };
    /// <summary>
    /// Saves custom configuration.
    /// </summary>
    private Dictionary<string, string> CaptureCustomConfiguration()
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (Control o in flowLayoutPanelCustomPluginSettings.Controls)
        {
            if (o is PluginSettingEditor setting)
            {
                values[setting.PropertyName] = setting.Value;
            }
        }
        return values;
    }

    /// <summary>
    /// Sets enabled mode.
    /// </summary>
    /// <param name="enabled">enabled 的值。</param>
    public void SetEnabledMode(bool enabled)
    {
        textBoxHardLockDuration.Enabled = enabled;
        textBoxHardLocks.Enabled = enabled;
        textBoxSoftLockDuration.Enabled = enabled;
        textBoxSoftLocks.Enabled = enabled;
        checkBoxLockForever.Enabled = enabled;
    }
    /// <summary>
    /// Clears errors.
    /// </summary>
    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }
    /// <summary>
    /// 處理儲存按鈕點擊事件。先顯示成功提示再發送變更通知，避免背景服務重啟延遲介面回應。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void pictureBoxSave_Click(object sender, EventArgs e)
    {
        if (_agent is null) return;
        buttonSave.Enabled = false;
        try
        {
            if (await SaveAgentChangesAsync(force: true))
                MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            if (!IsDisposed) buttonSave.Enabled = true;
        }
    }

    /// <summary>
    /// 儲存目前代理程式的異動並回傳是否確實寫入。呼叫端可依此決定是否顯示成功提示——
    /// <see cref="FlushUnsavedChangesAsync"/> 在切換代理程式時會靜默呼叫，不應跳出成功提示。
    /// </summary>
    /// <param name="force">即使目前未標示為待儲存，也擷取並持久化畫面設定。</param>
    /// <returns>若成功驗證並寫入設定則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    private async Task<bool> SaveAgentChangesAsync(bool force)
    {
        if (_agent is null) return true;
        if (!force && !_hasUnsavedChanges)
        {
            Task<bool>? pending = _pendingSaveTask;
            return pending is null || await pending;
        }

        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxHardLocks.Text, out int hardLocks))
        {
            errHardLocks.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxHardLockDuration.Text, out int hardLockDuration))
        {
            errHardLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLockDuration.Text, out int softLockDuration))
        {
            errSoftLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLocks.Text, out int softLocks))
        {
            errSoftLocks.Visible = true;
            hasError = true;
        }
        if (hasError) return false;

        Dictionary<string, string> customConfiguration = CaptureCustomConfiguration();
        if (!ValidateCustomConfiguration(customConfiguration)) return false;

        SecurityAgent agent = _agent;
        AgentSettingsSnapshot snapshot = new(
            agent, _editGeneration, agent.Id, agent.Name, agent.AssemblyName, agent.DisplayName, agent.Serial,
            hardLocks, hardLockDuration, checkBoxLockForever.Checked, softLocks, softLockDuration,
            checkBoxOverrideConfiguration.Checked, checkBoxEnableSecurityAgent.Checked,
            AgentSettingsSnapshot.Freeze(customConfiguration));

        if (_pendingSaveTask is not null && !_pendingSaveTask.IsCompleted && _pendingSaveGeneration == snapshot.EditGeneration)
            return await _pendingSaveTask;

        _pendingSaveGeneration = snapshot.EditGeneration;
        Task<bool> saveTask = PersistSnapshotAndApplyAsync(snapshot);
        _pendingSaveTask = saveTask;
        try
        {
            return await saveTask;
        }
        finally
        {
            if (ReferenceEquals(_pendingSaveTask, saveTask))
                _pendingSaveTask = null;
        }
    }

    private async Task<bool> PersistSnapshotAndApplyAsync(AgentSettingsSnapshot snapshot)
    {
        try
        {
            AgentSettingsSaveResult result = await _saveQueue.EnqueueAsync(snapshot, _lifetimeCancellation.Token);
            ApplyPersistedSnapshot(result);
            OnAgentConfigurationChanged();
            return true;
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode is 5 or 6)
        {
            ShowSaveError(Strings.Get("Database is currently busy. Please wait a moment and try saving again."), MessageBoxIcon.Warning);
            return false;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Agent configuration save failed: {0}", exception);
            ShowSaveError(Strings.Get("The configuration could not be saved. Please try again."), MessageBoxIcon.Error);
            return false;
        }
    }

    private void ShowSaveError(string message, MessageBoxIcon icon)
    {
        if (saveErrorPresenter is not null)
            saveErrorPresenter(this, message, icon);
        else
            MessageBox.Show(this, message, Strings.AppTitle, MessageBoxButtons.OK, icon);
    }

    private static Task<AgentSettingsSaveResult> PersistSnapshotAsync(AgentSettingsSnapshot snapshot, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            SecurityAgent persistenceAgent = snapshot.CreatePersistenceAgent();
            persistenceAgent.SaveInteractive(TimeSpan.FromSeconds(3), cancellationToken);
            return new AgentSettingsSaveResult(snapshot, persistenceAgent.Id, persistenceAgent.Serial);
        }, cancellationToken);

    private void ApplyPersistedSnapshot(AgentSettingsSaveResult result)
    {
        AgentSettingsSnapshot snapshot = result.Snapshot;
        SecurityAgent target = snapshot.Target;
        target.Id = result.PersistedId;
        target.Serial = result.PersistedSerial;
        target.HardLockAttempts = snapshot.HardLockAttempts;
        target.HardLockTimeHours = snapshot.HardLockTimeHours;
        target.LockForever = snapshot.LockForever;
        target.SoftLockAttempts = snapshot.SoftLockAttempts;
        target.SoftLockTimeMinutes = snapshot.SoftLockTimeMinutes;
        target.OverrideConfig = snapshot.OverrideConfig;
        target.Enabled = snapshot.Enabled;
        target.CustomConfiguration = new Dictionary<string, string>(snapshot.CustomConfiguration, StringComparer.Ordinal);

        if (ReferenceEquals(_agent, target) && _editGeneration == snapshot.EditGeneration)
            SetEditMode(false);
    }

    private bool ValidateCustomConfiguration(IReadOnlyDictionary<string, string> values)
    {
        if (TryInteger(values, "WindowSeconds", out int windowSeconds)
            && TryInteger(values, "SourceStateRetentionSeconds", out int retentionSeconds)
            && retentionSeconds < windowSeconds)
            return ShowCustomValidationError("Source state retention must not be shorter than the detection window.");

        if (values.TryGetValue("LogDirectory", out string? directory)
            && (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory)))
            return ShowCustomValidationError("Select an absolute log directory.");

        if (values.TryGetValue("LogFilePath", out string? filePath)
            && !string.IsNullOrWhiteSpace(filePath)
            && !Path.IsPathFullyQualified(filePath))
            return ShowCustomValidationError("Select an absolute log file path.");

        if (values.TryGetValue("SearchPattern", out string? pattern)
            && (string.IsNullOrWhiteSpace(pattern) || pattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            return ShowCustomValidationError("Enter a valid log file search pattern.");

        if (values.TryGetValue("ReadEventLog", out string? readEventLog)
            && bool.TryParse(readEventLog, out bool shouldReadEventLog)
            && !shouldReadEventLog
            && values.TryGetValue("LogFilePath", out filePath)
            && string.IsNullOrWhiteSpace(filePath))
            return ShowCustomValidationError("Enable Windows event log reading or select a log file.");

        return true;
    }

    private static bool TryInteger(IReadOnlyDictionary<string, string> values, string key, out int result)
    {
        result = default;
        return values.TryGetValue(key, out string? value)
            && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private bool ShowCustomValidationError(string key)
    {
        MessageBox.Show(this, Strings.Get(key), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }
    /// <summary>
    /// Processes the agent configuration changed notification.
    /// </summary>
    private void OnAgentConfigurationChanged() => AgentConfigurationChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// Processes the agent changed notification.
    /// </summary>
    private void OnAgentChanged() => AgentChanged?.Invoke(this, EventArgs.Empty);

    private SecurityAgent? _agent;
    private readonly AgentSettingsSaveQueue _saveQueue;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private Task<bool>? _pendingSaveTask;
    private long _pendingSaveGeneration = -1;
    private long _editGeneration;
        /// <summary>
    /// 取得或設定 Agent。
    /// </summary>
public SecurityAgent Agent
    {
        get => _agent ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Security agent has not been assigned."));
        set
        {
            _agent = value;
            AgentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 自動刷寫並持久化當前控制項中尚未儲存的 Agent 設定變更。
    /// </summary>
    public async Task<bool> FlushUnsavedChangesAsync()
    {
        return _agent is null || await SaveAgentChangesAsync(force: false);
    }

    /// <summary>
    /// 先非同步儲存目前 Agent 的待存異動，再切換至指定 Agent。
    /// </summary>
    /// <param name="agent">要顯示的 Agent。</param>
    /// <returns>若儲存成功並完成切換則傳回 <see langword="true"/>。</returns>
    public async Task<bool> SwitchAgentAsync(SecurityAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        if (ReferenceEquals(_agent, agent)) return true;
        if (!await FlushUnsavedChangesAsync()) return false;
        _agent = agent;
        OnAgentChanged();
        return true;
    }
    /// <summary>
    /// 將目前 Agent 的設定載入原廠預設值，等待使用者儲存或取消。
    /// </summary>
    private void ResetDefaults_Click(object? sender, EventArgs e)
    {
        ClearErrors();
        textBoxHardLocks.Text = IddsConfig.DefaultHardLockAttempts.ToString();
        textBoxHardLockDuration.Text = IddsConfig.DefaultHardLockHours.ToString();
        textBoxSoftLocks.Text = IddsConfig.DefaultSoftLockAttempts.ToString();
        textBoxSoftLockDuration.Text = IddsConfig.DefaultSoftLockMinutes.ToString();
        checkBoxLockForever.Checked = false;
        checkBoxOverrideConfiguration.Checked = false;
        SetEnabledMode(false);
        LoadCustomSettings(Agent.DefaultCustomConfiguration);
        smartLabelCustomConfig.Visible = flowLayoutPanelCustomPluginSettings.Controls.Count > 0;
        SetEditMode(true);
    }
    /// <summary>
    /// 處理 checked changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void checkBoxOverrideConfiguration_CheckedChanged(object sender, EventArgs e)
    {
        SetEnabledMode(checkBoxOverrideConfiguration.Checked);
        if (_isLoadingData || _agent is null) return;
        SetEditMode(true);
    }

    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void textBox_KeyPress(object? sender, KeyPressEventArgs e) => SetEditMode(true);

    private void textBox_TextChanged(object? sender, EventArgs e)
    {
        if (_isLoadingData || _agent is null) return;
        SetEditMode(true);
    }
    private bool _hasUnsavedChanges;
    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>
    private void SetEditMode(bool hasChanges)
    {
        if (hasChanges) _editGeneration++;
        _hasUnsavedChanges = hasChanges;
    }
    /// <summary>
    /// 處理 checked changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void checkBox_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoadingData || _agent is null) return;
        SetEditMode(true);
    }

}

using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

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
        : this(null)
    {
    }

    /// <summary>
    /// 初始化可指定恢復預設值確認提示的設定面板。
    /// </summary>
    /// <param name="confirmationPrompt">確認提示委派；正式執行時傳入 <see langword="null"/>。</param>
    internal PanelPluginConfiguration(Func<IWin32Window, DialogResult>? confirmationPrompt)
    {
        InitializeComponent();
        flowLayoutPanelCustomPluginSettings.ClientSizeChanged += (_, _) => UpdateCustomSettingsLayout();
        AgentChanged += new EventHandler(PanelPluginConfiguration_AgentChanged);
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click, confirmationPrompt: confirmationPrompt);
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
        ClearErrors();
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X + 1, pictureBox.Location.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X - 1, pictureBox.Location.Y - 1);
    }

        /// <summary>
    /// 取得或設定 IsInEditMode。
    /// </summary>
public bool IsInEditMode { get; set; }
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
            if (IsInEditMode) ToggleEditMode();
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
    private void SaveCustomConfiguration()
    {
        foreach (Control o in flowLayoutPanelCustomPluginSettings.Controls)
        {
            if (o is PluginSettingEditor setting)
            {
                Agent.CustomConfiguration[setting.PropertyName] = setting.Value;
            }
        }
    }

    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData(); else ToggleEditMode();
        ClearErrors();
    }
    /// <summary>
    /// 執行 toggle edit mode 作業。
    /// </summary>
    private void ToggleEditMode()
    {
        IsInEditMode = true;
        return;
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
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxSave_Click(object sender, EventArgs e)
    {
        if (_agent is null) return;
        SaveAgentChanges(_agent);
    }

    private void SaveAgentChanges(SecurityAgent agent)
    {
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
        if (!hasError)
        {
            agent.LockForever = checkBoxLockForever.Checked;
            agent.HardLockAttempts = hardLocks;
            agent.HardLockTimeHours = hardLockDuration;
            agent.SoftLockAttempts = softLocks;
            agent.SoftLockTimeMinutes = softLockDuration;
            agent.Enabled = checkBoxEnableSecurityAgent.Checked;
            agent.OverrideConfig = checkBoxOverrideConfiguration.Checked;
            SaveCustomConfiguration();
            if (!ValidateCustomConfiguration())
            {
                SetEditMode(true);
                return;
            }
            agent.Save();
            OnAgentConfigurationChanged();
        }
        SetEditMode(false);
    }

    private bool ValidateCustomConfiguration()
    {
        Dictionary<string, string> values = Agent.CustomConfiguration;
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
        /// <summary>
    /// 取得或設定 Agent。
    /// </summary>
public SecurityAgent Agent
    {
        get => _agent ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Security agent has not been assigned."));
        set
        {
            FlushUnsavedChanges();
            _agent = value;
            AgentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 自動刷寫並持久化當前控制項中尚未儲存的 Agent 設定變更。
    /// </summary>
    public void FlushUnsavedChanges()
    {
        if (_agent != null && buttonSave.Visible)
        {
            SaveAgentChanges(_agent);
        }
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();
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
    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>
    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
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

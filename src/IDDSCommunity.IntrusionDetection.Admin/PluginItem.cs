using System;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 顯示個別安全性代理程式基本資訊與狀態之單元控制項。
/// </summary>
public partial class PluginItem : UserControl
{
    private string? renderedDisplayName;
    private bool? renderedEnabled;
    private bool iconInitialized;

        /// <summary>
    /// 當 SecurityAgentConfigurationRequest 時引發之事件。
    /// </summary>
public event EventHandler? SecurityAgentConfigurationRequest;
    /// <summary>
    /// 初始化 <see cref="PluginItem"/> 類別的新執行個體。
    /// </summary>
    public PluginItem()
    {
        InitializeComponent();
        AccessibleRole = AccessibleRole.Grouping;
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Focused)
        {
            using Pen focusPen = new(Color.FromArgb(19, 184, 166), 1.5F) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            Rectangle focusRect = new(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            e.Graphics.DrawRectangle(focusPen, focusRect);
        }
    }
    /// <summary>
    /// Sets soft locks.
    /// </summary>
    /// <param name="softLocks">soft locks 的值。</param>
    public void SetSoftLocks(int softLocks) => labelSoftLocksValue.Text = softLocks.ToString();

    /// <summary>
    /// Sets hard locks.
    /// </summary>
    /// <param name="hardLocks">hard locks 的值。</param>
    public void SetHardLocks(int hardLocks) => labelHardLocksValue.Text = hardLocks.ToString();
    /// <summary>
    /// Sets name.
    /// </summary>
    /// <param name="name">name 的值。</param>
    public void SetName(string name) => labelAgentName.Text = name;
    /// <summary>
    /// Sets icon.
    /// </summary>
    /// <param name="icon">icon 的值。</param>
    public void SetIcon(Image? icon) => pictureBoxAgentIcon.Image = icon;

    /// <summary>
    /// Sets failed logins.
    /// </summary>
    /// <param name="failedLogins">failed logins 的值。</param>
    public void SetFailedLogins(int failedLogins) => labelFailedLoginsValue.Text = failedLogins.ToString();

    private SecurityAgent? _securityAgent;
        /// <summary>
    /// 取得或設定 SecurityAgent。
    /// </summary>
public SecurityAgent SecurityAgent
    {
        get => _securityAgent ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Security agent has not been assigned."));
        set
        {
            if (!ReferenceEquals(_securityAgent, value))
            {
                Image? previousAgentIcon = pictureBoxAgentIcon.Image;
                pictureBoxAgentIcon.Image = null;
                previousAgentIcon?.Dispose();
                renderedDisplayName = null;
                renderedEnabled = null;
                iconInitialized = false;
            }
            _securityAgent = value;
            RefreshPresentation();
            SetStatistics(0, 0, 0);
        }
    }
    /// <summary>
    /// 依目前 Agent 狀態重新整理名稱、圖示及啟用狀態。
    /// </summary>
    public void RefreshPresentation()
    {
        string displayName = SecurityAgent.DisplayName;
        bool enabled = SecurityAgent.Enabled;
        bool presentationChanged = false;
        if (!string.Equals(renderedDisplayName, displayName, StringComparison.Ordinal))
        {
            SetName(displayName);
            renderedDisplayName = displayName;
            presentationChanged = true;
        }
        if (!iconInitialized)
        {
            SetIcon(SecurityAgent.Icon);
            iconInitialized = true;
            presentationChanged = true;
        }
        if (renderedEnabled != enabled)
        {
            Image? previousStatusImage = pictureBoxEnabledState.Image;
            pictureBoxEnabledState.Image = InterfaceIcons.CreateAgentStatus(16, enabled);
            previousStatusImage?.Dispose();
            renderedEnabled = enabled;
            presentationChanged = true;
        }
        if (!presentationChanged)
            return;

        string localizedStatus = Strings.Get(enabled ? "enabled" : "disabled");
        AccessibleName = displayName;
        AccessibleDescription = Strings.Format(
            "The security agent {0} is {1}. Double-click to configure this agent.",
            displayName,
            localizedStatus);
        pictureBoxEnabledState.AccessibleName = $"{Strings.Get("Agent status")}: {localizedStatus}";
        pictureBoxEnabledState.AccessibleDescription = AccessibleDescription;
        toolTip1.ToolTipTitle = Strings.Get("Agent status");
        toolTip1.SetToolTip(pictureBoxEnabledState, pictureBoxEnabledState.AccessibleDescription);
    }
    /// <summary>
    /// 設定 Dashboard 顯示的 Agent 統計資料。
    /// </summary>
    /// <param name="failedLogins">指定時間區間內的偵測事件數。</param>
    /// <param name="hardLocks">強制封鎖累計數。</param>
    /// <param name="softLocks">暫時封鎖累計數。</param>
    public void SetStatistics(int failedLogins, int hardLocks, int softLocks)
    {
        SetFailedLogins(failedLogins);
        SetHardLocks(hardLocks);
        SetSoftLocks(softLocks);
    }
    /// <summary>
    /// 處理 double click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEnabledState_DoubleClick(object sender, EventArgs e) => SecurityAgentConfigurationRequest?.Invoke(SecurityAgent, EventArgs.Empty);



}

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// IDDS 社群版主管理主控台視窗。
/// </summary>
public partial class IddsAdmin : Form
{
    private const string ServiceName = Globals.WINDOWS_SERVICE_NAME;
    private static readonly TimeSpan SecurityLogWindow = TimeSpan.FromDays(30);
    private static readonly TimeSpan SecurityLogRefreshInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DashboardRefreshInterval = TimeSpan.FromSeconds(10);
    private DateTime lastDashboardRefresh = DateTime.MinValue;
    private int lastDashboardLogId = -1;
    readonly Color buttonHighlight = Color.FromArgb(205, 230, 247);
    readonly Color buttonPress = Color.FromArgb(105, 130, 147);
    readonly Color buttonNormal = Color.FromKnownColor(KnownColor.Window);
    Timer? logReader;
    Timer? timerRefreshServiceStatus;
    IDDSCommunitySecurityLog? _panelSecurityLog;
    IDDSCommunityCurrentLocks? _panelCurrentLocks;
    IDDSCommunityDashboard? _dashboard;
    IDDSCommunityAgentConfiguration? _panelAgentConfiguration;
    IDDSCommunityApplicationSettings? _panelApplicationSettings;
    PanelSystemOperationsLog? _panelSystemOperationsLog;

    System.ServiceProcess.ServiceController? serviceController;
    private EventLog? eventLogIDDSCommunity;
    private readonly System.Threading.CancellationTokenSource uiRefreshCancellation = new();
    private readonly System.Threading.SemaphoreSlim serviceOperationGate = new(1, 1);
    private int serviceRefreshActive;
    private Bitmap? disabledStartServiceImage;
    private Bitmap? disabledStopServiceImage;
    private DateTime lastSecurityLogRefresh = DateTime.MinValue;
    /// <summary>
    /// 初始化 <see cref="IddsAdmin"/> 類別的新執行個體。
    /// </summary>
    public IddsAdmin()
    {
        InitializeComponent();
        UpdateMenuPositions();
        UpdateTitleBarPositions();
        panelMenu.Resize += (s, e) => UpdateMenuPositions();
        panelWindowGrip.Resize += (s, e) => UpdateTitleBarPositions();
        Icon = BrandingIcons.CreateIcon();
        BrandingIcons.ApplyTo(pictureBox1);
        string version = typeof(IddsAdmin).Assembly.GetName().Version?.ToString(3) ?? "3.0.0";
        Text = Strings.Format("IDDSCommunity Intrusion Detection - Version {0}", version);
        labelFormText.Text = Text;

        //            panelContent.Invalidated += new InvalidateEventHandler(panelContent_Invalidated);
        panelContent.Paint += new PaintEventHandler(panelContent_Paint);

        Load += new EventHandler(IddsAdmin_Load);
    }

    /// <summary>
    /// 依據目前語系與字元寬度自適應排列頂部選單按鈕與右側服務控制項，避免按鈕重疊或破邊遮蔽。
    /// </summary>
    public void UpdateMenuPositions()
    {
        SmartLabel[] menus = [labelMenuHome, labelMenuCurrentLocks, labelMenuSecurityLog, labelMenuSystemLog, labelMenuAgents, labelMenuSettings];
        int currentLeft = 4;
        foreach (SmartLabel menu in menus)
        {
            menu.Location = new Point(currentLeft, 14);
            currentLeft += menu.Width;
        }

        int right = panelMenu.ClientSize.Width - 12;
        pictureBoxStopService.Location = new Point(right - pictureBoxStopService.Width, 14);
        right = pictureBoxStopService.Left - 7;
        pictureBoxStartService.Location = new Point(right - pictureBoxStartService.Width, 14);
        right = pictureBoxStartService.Left - 10;
        buttonManageService.Location = new Point(right - buttonManageService.Width, 10);
        right = buttonManageService.Left - 10;
        int statusWidth = Math.Max(80, right - currentLeft - 10);
        smartLabelServiceStatus.Location = new Point(right - statusWidth, 14);
        smartLabelServiceStatus.Size = new Size(statusWidth, 20);
    }

    /// <summary>
    /// 動態調整標題列按鈕與標題文字位置，確保標題列控制項不重疊且隨視窗大小自適應。
    /// </summary>
    public void UpdateTitleBarPositions()
    {
        int right = panelWindowGrip.ClientSize.Width - 12;
        pictureBoxCloseButton.Location = new Point(right - pictureBoxCloseButton.Width, 4);
        right = pictureBoxCloseButton.Left - 11;
        pictureBoxMaximizeButton.Location = new Point(right - pictureBoxMaximizeButton.Width, 4);
        right = pictureBoxMaximizeButton.Left - 11;
        pictureBoxMinimizeButton.Location = new Point(right - pictureBoxMinimizeButton.Width, 4);
        right = pictureBoxMinimizeButton.Left - 11;
        pictureBoxHelpButon.Location = new Point(right - pictureBoxHelpButon.Width, 4);

        int availableTextWidth = pictureBoxHelpButon.Left - labelFormText.Left - 10;
        if (availableTextWidth > 50)
        {
            labelFormText.Width = availableTextWidth;
        }
    }
    /// <summary>
    /// Cancels pending background snapshots before WinForms destroys control handles.
    /// </summary>
    /// <param name="e">The form-close event data.</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        uiRefreshCancellation.Cancel();
        logReader?.Dispose();
        timerRefreshServiceStatus?.Dispose();
        eventLogIDDSCommunity?.Dispose();
        uiRefreshCancellation.Dispose();
        serviceOperationGate.Dispose();
        disabledStartServiceImage?.Dispose();
        disabledStopServiceImage?.Dispose();
        serviceController?.Dispose();
        base.OnFormClosed(e);
    }

    private static IddsAdmin? _instance;
    /// <summary>
    /// 取得或設定 全域共用單例執行個體。
    /// </summary>
    public static IddsAdmin Instance
    {
        get
        {
            _instance ??= new IddsAdmin
            {
                Visible = false
            };
            return _instance;
        }
    }


    /// <summary>
    /// 取得或設定 PanelApplicationSettings。
    /// </summary>
    public IDDSCommunityApplicationSettings PanelApplicationSettings
    {
        get
        {
            if (_panelApplicationSettings == null)
            {
                _panelApplicationSettings = new IDDSCommunityApplicationSettings
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelApplicationSettings);

                _panelApplicationSettings.ConfigurationChanged += new EventHandler(_panelApplicationSettings_ConfigurationChanged);
            }
            return _panelApplicationSettings;
        }
    }
    /// <summary>
    /// 處理 configuration changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    async void _panelApplicationSettings_ConfigurationChanged(object? sender, EventArgs e) => await RestartServiceAsync();

    /// <summary>
    /// 取得或設定 PanelAgentConfiguration。
    /// </summary>
    public IDDSCommunityAgentConfiguration PanelAgentConfiguration
    {
        get
        {
            if (_panelAgentConfiguration == null)
            {
                _panelAgentConfiguration = new IDDSCommunityAgentConfiguration
                {
                    Dock = DockStyle.Fill
                };
                _panelAgentConfiguration.PluginsChanged += new EventHandler(_panelAgentConfiguration_PluginsChanged);
                _panelAgentConfiguration.AgentSettingsChanged += new EventHandler(_panelAgentConfiguration_AgentSettingsChanged);
                panelContent.Controls.Add(_panelAgentConfiguration);
            }
            return _panelAgentConfiguration;
        }
    }
    /// <summary>
    /// 處理 agent settings changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    async void _panelAgentConfiguration_AgentSettingsChanged(object? sender, EventArgs e)
    {
        Dashboard.RefreshAgentPresentations();
        await RestartServiceAsync();
    }
    /// <summary>
    /// 處理 plugins changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void _panelAgentConfiguration_PluginsChanged(object? sender, EventArgs e) => InitAgentSettings();//RestartService();
    /// <summary>
    /// 執行 restart service 作業。
    /// </summary>
    public async Task RestartServiceAsync()
    {
        bool gateEntered = false;
        try
        {
            await serviceOperationGate.WaitAsync(uiRefreshCancellation.Token).ConfigureAwait(false);
            gateEntered = true;
            System.ServiceProcess.ServiceController? controller = serviceController;
            if (controller is null)
            {
                if (!IsDisposed && IsHandleCreated)
                    await this.InvokeAsync(() => ApplyServiceStatus(null), uiRefreshCancellation.Token);
                return;
            }
            await ElevatedServiceCommand.RunElevatedAsync(ServiceName, "restart", uiRefreshCancellation.Token).ConfigureAwait(false);
            System.ServiceProcess.ServiceControllerStatus status = await Task.Run(() =>
            {
                controller.Refresh();
                return controller.Status;
            }, uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyServiceStatus(status), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            System.ServiceProcess.ServiceController? controller = serviceController;
            MarkServiceUnavailable(controller, ex);
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() =>
                {
                    ApplyServiceStatus(null);
                    MessageBox.Show(this, Strings.Get("The service change could not be completed."),
                        Strings.Get("Service operation failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
        }
        finally
        {
            if (gateEntered) serviceOperationGate.Release();
        }
    }

    /// <summary>
    /// 取得或設定 PanelSecurityLog。
    /// </summary>
    public IDDSCommunitySecurityLog PanelSecurityLog
    {
        get
        {
            if (_panelSecurityLog == null)
            {
                _panelSecurityLog = new IDDSCommunitySecurityLog
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelSecurityLog);
                foreach (SecurityAgent agent in SecurityAgents.Instance)
                {
                    _panelSecurityLog.AddAgent(agent);
                }
                // 控制項建立本身維持同步（WinForms 限制），但初始資料改走與 logReader_Tick 相同的
                // 背景快照＋InvokeAsync 模式非同步載入，避免第一次存取此屬性時同步阻塞呼叫端執行緒
                // （包含 SplashScreen 的預先載入呼叫）。
                _ = LoadInitialSecurityLogAsync();
            }
            return _panelSecurityLog;
        }
    }
    /// <summary>
    /// 以背景快照模式非同步載入安全性記錄面板的初始資料，並透過 <c>Control.InvokeAsync</c>
    /// 將結果套用回 UI 執行緒，避免 <see cref="PanelSecurityLog"/> 首次存取時同步阻塞呼叫端。
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    private async Task LoadInitialSecurityLogAsync()
    {
        if (!Database.Instance.IsConfigured)
            return;
        IsUpdating = true;
        try
        {
            AdminRefreshSnapshot snapshot = await Task.Run(
                () => LoadAdminSnapshot(AdminRefreshMode.SecurityLog, LastLogId, LastLockUpdate, lastSecurityLogRefresh, lastDashboardRefresh, lastDashboardLogId),
                uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyAdminSnapshot(snapshot), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (InvalidOperationException) when (IsDisposed || !IsHandleCreated) { }
        catch (Exception ex)
        {
            try
            {
                EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Admin", ex.Message, EventLogEntryType.Error);
            }
            catch (Exception logException)
            {
                Trace.TraceError("Initial security log load failed: {0}{1}Event Log write failed: {2}", ex, Environment.NewLine, logException);
                _ = RollingDiagnosticLog.Write("Admin-Refresh", "Initial security log load and Event Log write failed", ex);
            }
        }
        finally
        {
            try
            {
                if (!IsDisposed && !uiRefreshCancellation.IsCancellationRequested)
                {
                    if (IsHandleCreated)
                    {
                        await this.InvokeAsync(() =>
                        {
                            IsUpdating = false;
                        }, uiRefreshCancellation.Token);
                    }
                    else
                    {
                        // The security-log panel is preloaded while the splash screen is still
                        // active. The administration form may not have a window handle yet, but
                        // the refresh gate must still be released for the first dashboard tick.
                        IsUpdating = false;
                    }
                }
            }
            catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
            catch (InvalidOperationException) when (IsDisposed || !IsHandleCreated) { }
        }
    }
    /// <summary>
    /// 執行 fill log 作業。
    /// </summary>
    /// <param name="rdr">rdr 的值。</param>
    private static void FillLog(IDataReader rdr)
    {
    }

    /// <summary>
    /// 取得或設定 PanelCurrentLocks。
    /// </summary>
    public IDDSCommunityCurrentLocks PanelCurrentLocks
    {
        get
        {
            if (_panelCurrentLocks == null)
            {
                _panelCurrentLocks = new IDDSCommunityCurrentLocks
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelCurrentLocks);
            }
            return _panelCurrentLocks;
        }
    }

    /// <summary>
    /// 取得系統內部作業稽核日誌檢視面板之執行個體。
    /// </summary>
    public PanelSystemOperationsLog PanelSystemOperationsLog
    {
        get
        {
            if (_panelSystemOperationsLog == null)
            {
                _panelSystemOperationsLog = new PanelSystemOperationsLog
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelSystemOperationsLog);
            }
            return _panelSystemOperationsLog;
        }
    }

    /// <summary>
    /// 取得或設定 Dashboard。
    /// </summary>
    public IDDSCommunityDashboard Dashboard
    {
        get
        {
            if (_dashboard == null)
            {
                _dashboard = new IDDSCommunityDashboard
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_dashboard);
                _dashboard.SecurityAgentConfigurationRequest += new EventHandler(_dashboard_SecurityAgentConfigurationRequest);
            }
            return _dashboard;
        }
    }
    /// <summary>
    /// 處理 security agent configuration request 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void _dashboard_SecurityAgentConfigurationRequest(object? sender, EventArgs e)
    {
        if (sender != null && sender is SecurityAgent)
        {
            ShowMenu(labelMenuAgents);
            if (sender is SecurityAgent agent)
                PanelAgentConfiguration.ShowAgentConfig(agent);
            ShowContentPanel(PanelAgentConfiguration);
            panelOnlineServices.Hide();
        }
    }

    /// <summary>
    /// 取得或設定 IsServiceRunning。
    /// </summary>
    public bool IsServiceRunning { get; set; }
    /// <summary>
    /// 處理 tick 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    async void serviceReader_Tick(object? sender, EventArgs e)
    {
        if (System.Threading.Interlocked.Exchange(ref serviceRefreshActive, 1) != 0)
            return;
        bool gateEntered = false;
        try
        {
            await serviceOperationGate.WaitAsync(uiRefreshCancellation.Token).ConfigureAwait(false);
            gateEntered = true;
            System.ServiceProcess.ServiceControllerStatus? status = await Task.Run(ReadServiceStatus, uiRefreshCancellation.Token).ConfigureAwait(false);
            if (!IsDisposed)
                await this.InvokeAsync(() =>
                {
                    ApplyServiceStatus(status);
                }, uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Trace.TraceError("Service status refresh failed: {0}", exception);
            ServiceError = true;
        }
        finally
        {
            if (gateEntered)
                serviceOperationGate.Release();
            System.Threading.Interlocked.Exchange(ref serviceRefreshActive, 0);
        }
    }


    /// <summary>
    /// 取得或設定 ServiceError。
    /// </summary>
    public bool ServiceError { get; set; }
    /// <summary>
    /// Reads the current Windows service status without accessing UI controls.
    /// </summary>
    /// <returns>服務狀態；若無控制器則傳回 <see langword="null"/>。</returns>
    private System.ServiceProcess.ServiceControllerStatus? ReadServiceStatus()
    {
        System.ServiceProcess.ServiceController? controller = serviceController;
        if (controller is null)
        {
            controller = new System.ServiceProcess.ServiceController(ServiceName);
            serviceController = controller;
        }
        if (WindowsServiceStatusReader.TryRead(() =>
        {
            controller.Refresh();
            return controller.Status;
        }, out System.ServiceProcess.ServiceControllerStatus status, out Exception? failure))
        {
            ServiceError = false;
            return status;
        }
        if (failure is not null)
        {
            MarkServiceUnavailable(controller, failure);
        }
        return null;
    }
    /// <summary>
    /// Applies one service-status snapshot on the UI thread.
    /// </summary>
    private const char StatusDot = '●';

    private static Bitmap CreateDisabledImage(Image original)
    {
        Bitmap bitmap = new(original.Width, original.Height);
        using Graphics g = Graphics.FromImage(bitmap);
        System.Drawing.Imaging.ColorMatrix colorMatrix = new([
            [0.3f, 0.3f, 0.3f, 0, 0],
            [0.59f, 0.59f, 0.59f, 0, 0],
            [0.11f, 0.11f, 0.11f, 0, 0],
            [0, 0, 0, 0.3f, 0],
            [0, 0, 0, 0, 1]
        ]);
        using System.Drawing.Imaging.ImageAttributes attributes = new();
        attributes.SetColorMatrix(colorMatrix);
        g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
        return bitmap;
    }

    private Image DisabledStartServiceImage =>
        disabledStartServiceImage ??= CreateDisabledImage(Properties.Resources.service_controller_start);

    private Image DisabledStopServiceImage =>
        disabledStopServiceImage ??= CreateDisabledImage(Properties.Resources.service_controller_stop);
    /// <summary>
    /// Applies background-refreshed status to the header indicators and controls.
    /// </summary>
    /// <param name="status">The status read by the background operation.</param>
    private void ApplyServiceStatus(System.ServiceProcess.ServiceControllerStatus? status)
    {
        smartLabelServiceStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        if (ServiceError)
        {
            string notFound = Strings.Get("Service not found!");
            smartLabelServiceStatus.Text = StatusDot + " " + notFound;
            smartLabelServiceStatus.ForeColor = Color.FromArgb(239, 68, 68);
            buttonManageService.Text = Strings.Get("Install service");
            pictureBoxStartService.Image = DisabledStartServiceImage;
            pictureBoxStopService.Image = DisabledStopServiceImage;
            pictureBoxStartService.Enabled = false;
            pictureBoxStopService.Enabled = false;
            pictureBoxStartService.Cursor = Cursors.Default;
            pictureBoxStopService.Cursor = Cursors.Default;
            UpdateMenuPositions();
            return;
        }
        buttonManageService.Text = Strings.Get("Uninstall service");
        try
        {
            if (status == System.ServiceProcess.ServiceControllerStatus.Running)
            {
                IsServiceRunning = true;
                pictureBoxStartService.Image = DisabledStartServiceImage;
                pictureBoxStartService.Enabled = false;
                pictureBoxStartService.Cursor = Cursors.Default;
                pictureBoxStopService.Image = Properties.Resources.service_controller_stop;
                pictureBoxStopService.Enabled = true;
                pictureBoxStopService.Cursor = Cursors.Hand;
                string running = Strings.Get("Service is running");
                smartLabelServiceStatus.Text = StatusDot + " " + running;
                smartLabelServiceStatus.ForeColor = Color.FromArgb(16, 185, 129); // Vibrant Emerald Green
            }
            else if (status == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                IsServiceRunning = false;
                pictureBoxStartService.Image = Properties.Resources.service_controller_start;
                pictureBoxStartService.Enabled = true;
                pictureBoxStartService.Cursor = Cursors.Hand;
                pictureBoxStopService.Image = DisabledStopServiceImage;
                pictureBoxStopService.Enabled = false;
                pictureBoxStopService.Cursor = Cursors.Default;
                string stopped = Strings.Get("Service is stopped");
                smartLabelServiceStatus.Text = StatusDot + " " + stopped;
                smartLabelServiceStatus.ForeColor = Color.FromArgb(239, 68, 68); // Vibrant Crimson Red
            }
            else
            {
                string reading = Strings.Get("reading status....");
                smartLabelServiceStatus.Text = StatusDot + " " + reading;
                smartLabelServiceStatus.ForeColor = Color.FromArgb(245, 158, 11); // Amber Yellow
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError("Applying service status failed: {0}", exception);
            ServiceError = true;
        }
        UpdateMenuPositions();
    }

    /// <summary>
    /// 取得或設定 IsUpdating。
    /// </summary>
    public bool IsUpdating { get; set; }
    /// <summary>
    /// Gets log message.
    /// </summary>
    /// <param name="agentId">agent id 的值。</param>
    /// <param name="action">action 的值。</param>
    /// <returns>取得的日誌訊息內容。</returns>
    public static string GetLogMessage(string agentId, int action)
    {
        string agentName = SecurityAgents.Instance.GetDisplayName(agentId);
        string message = string.Empty;
        if (action <= IntrusionLog.STATUS_SOFT_LOCK_REQUESTED || action == IntrusionLog.STATUS_HARD_LOCK_REQUESTED)
        {
            message = string.Format("{0}: ", agentName);
        }
        return string.Format("{0}{1}", message, IntrusionLog.GetStatusName(action));
    }
    /// <summary>
    /// 處理 tick 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    async void logReader_Tick(object? sender, EventArgs e)
    {
        if (IsUpdating || !Database.Instance.IsConfigured)
            return;
        IsUpdating = true;
        AdminRefreshMode mode = CurrentMenu == labelMenuSecurityLog
            ? AdminRefreshMode.SecurityLog
            : CurrentMenu == labelMenuCurrentLocks
                ? AdminRefreshMode.CurrentLocks
                : CurrentMenu == labelMenuHome
                    ? AdminRefreshMode.Dashboard
                    : AdminRefreshMode.None;
        int lastLogId = LastLogId;
        DateTime lastLockUpdate = LastLockUpdate;
        DateTime securityLogRefresh = lastSecurityLogRefresh;
        DateTime dashboardRefresh = lastDashboardRefresh;
        int dashboardLogId = lastDashboardLogId;
        try
        {
            AdminRefreshSnapshot snapshot = await Task.Run(() => LoadAdminSnapshot(mode, lastLogId, lastLockUpdate, securityLogRefresh, dashboardRefresh, dashboardLogId), uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyAdminSnapshot(snapshot), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            try
            {
                EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Admin", ex.Message, EventLogEntryType.Error);
            }
            catch (Exception logException)
            {
                Trace.TraceError("Admin refresh failed: {0}{1}Event Log write failed: {2}", ex, Environment.NewLine, logException);
                _ = RollingDiagnosticLog.Write("Admin-Refresh", "Admin refresh and Event Log write failed", ex);
            }
        }
        finally
        {
            try
            {
                if (!IsDisposed && !uiRefreshCancellation.IsCancellationRequested)
                {
                    await this.InvokeAsync(() =>
                    {
                        IsUpdating = false;
                    }, uiRefreshCancellation.Token);
                }
            }
            catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
            catch (InvalidOperationException) when (IsDisposed || !IsHandleCreated) { }
        }
    }
    /// <summary>
    /// Loads one immutable administration snapshot without accessing WinForms controls.
    /// </summary>
    /// <param name="mode">The visible administration area.</param>
    /// <param name="lastLogId">The last log identifier already displayed.</param>
    /// <param name="lastLockUpdate">The last lock refresh timestamp.</param>
    /// <param name="lastSecurityLogRefresh">上次完整載入安全性記錄時間。</param>
    /// <param name="lastDashboardRefresh">上次儀表板 30 天統計重新彙總時間。</param>
    /// <param name="lastDashboardLogId">上次儀表板彙總時的最大日誌識別碼。</param>
    /// <returns>背景載入的管理快照物件。</returns>
    private static AdminRefreshSnapshot LoadAdminSnapshot(AdminRefreshMode mode, int lastLogId, DateTime lastLockUpdate, DateTime lastSecurityLogRefresh, DateTime lastDashboardRefresh, int lastDashboardLogId)
    {
        List<AdminLogRow> logs = [];
        List<AdminLockRow> locks = [];
        int maxLogId = lastLogId;
        DateTime? newLockUpdate = null;
        DateTime? newSecurityLogRefresh = null;
        DateTime? newDashboardRefresh = null;
        int? newDashboardLogId = null;
        bool replaceSecurityLog = false;
        FailedLoginStatisticsSnapshot? failedLoginStatistics = null;
        IReadOnlyDictionary<Guid, AgentLockStatistics>? agentLockStatistics = null;
        int? softLocks = null;
        int? hardLocks = null;
        int? crossAgentAlerts = null;
        if (mode == AdminRefreshMode.SecurityLog)
        {
            DateTime endDate = DateTime.UtcNow;
            int currentLogId = IntrusionLog.GetLastLogId();
            if (SecurityLogRefreshPolicy.ShouldRefresh(endDate, lastSecurityLogRefresh, lastLogId, currentLogId, SecurityLogRefreshInterval))
            {
                replaceSecurityLog = true;
                newSecurityLogRefresh = endDate;
                maxLogId = currentLogId;
                using IDataReader reader = IntrusionLog.ReadIntervalGrouped(endDate.Subtract(SecurityLogWindow), endDate);
                while (reader.Read())
                {
                    int id = Shared.Db.DbValueConverter.ToInt(reader["MaxId"]);
                    int action = Shared.Db.DbValueConverter.ToInt(reader["Action"]);
                    string agentId = Shared.Db.DbValueConverter.ToString(reader["AgentId"]);
                    logs.Add(new AdminLogRow(
                        id,
                        action,
                        agentId,
                        Shared.Db.DbValueConverter.ToDateTime(reader["LatestEvent"]).ToLocalTime(),
                        Shared.Db.DbValueConverter.ToString(reader["ClientIP"]),
                        GetLogMessage(agentId, action),
                        Shared.Db.DbValueConverter.ToInt(reader["NumberOfEvents"])));
                }
            }
        }
        if (mode == AdminRefreshMode.CurrentLocks && Locks.HasUpdates(lastLockUpdate))
        {
            newLockUpdate = DateTime.UtcNow;
            using IDataReader reader = Locks.ReadLocks();
            while (reader.Read())
            {
                DateTime.TryParse(reader["LockDate"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime lockDateUtc);
                DateTime.TryParse(reader["UnlockDate"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime unlockDateUtc);
                DateTime lockDate = DateTime.SpecifyKind(lockDateUtc, DateTimeKind.Utc).ToLocalTime();
                DateTime unlockDate = DateTime.SpecifyKind(unlockDateUtc, DateTimeKind.Utc).ToLocalTime();
                int status = Shared.Db.DbValueConverter.ToInt(reader["Status"]);
                string agentId = Shared.Db.DbValueConverter.ToString(reader["AgentId"]);
                string displayName = Shared.Db.DbValueConverter.ToString(reader["DisplayName"]);
                if (!string.IsNullOrWhiteSpace(agentId))
                {
                    displayName = SecurityAgents.Instance.GetDisplayName(agentId);
                }
                else if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = Shared.Localization.Strings.Get("System");
                }
                locks.Add(new AdminLockRow(Shared.Db.DbValueConverter.ToInt(reader["LockId"]), status, Shared.Db.DbValueConverter.ToString(reader["ClientIp"]), displayName, lockDate, unlockDate));
            }
        }
        if (mode == AdminRefreshMode.Dashboard)
        {
            DateTime nowUtc = DateTime.UtcNow;
            int currentLogId = IntrusionLog.GetLastLogId();
            bool shouldRefreshDashboard = lastDashboardLogId < 0
                || currentLogId != lastDashboardLogId
                || (nowUtc - lastDashboardRefresh) >= DashboardRefreshInterval;

            if (shouldRefreshDashboard)
            {
                newDashboardRefresh = nowUtc;
                newDashboardLogId = currentLogId;
                DateTime endDate = nowUtc;
                DateTime startDate = endDate.AddDays(-30);
                failedLoginStatistics = Locks.ReadFailedLoginStatistics(startDate, endDate);
                agentLockStatistics = Locks.ReadAgentLockStatistics();
                crossAgentAlerts = Locks.ReadCrossAgentAlertCount(startDate, endDate);
                softLocks = Locks.ReadCurrentSoftLocks();
                hardLocks = Locks.ReadCurrentHardLocks();
            }
        }
        if (mode == AdminRefreshMode.CurrentLocks)
        {
            softLocks = Locks.ReadCurrentSoftLocks();
            hardLocks = Locks.ReadCurrentHardLocks();
        }
        return new AdminRefreshSnapshot(mode, logs, locks, maxLogId, newLockUpdate, newSecurityLogRefresh, replaceSecurityLog, failedLoginStatistics, agentLockStatistics, softLocks, hardLocks, crossAgentAlerts, newDashboardRefresh, newDashboardLogId);
    }
    /// <summary>
    /// Applies a background-loaded administration snapshot on the UI thread.
    /// </summary>
    /// <param name="snapshot">The immutable values to display.</param>
    private void ApplyAdminSnapshot(AdminRefreshSnapshot snapshot)
    {
        if (snapshot.ReplaceSecurityLog)
        {
            PanelSecurityLog.ClearEntries();
            foreach (AdminLogRow row in snapshot.Logs)
            {
                PanelSecurityLog.FillLogEntry(row.Id, row.Action, row.AgentId, IntrusionLog.GetStatusIcon(row.Action), IntrusionLog.GetStatusClass(row.Action), row.IncidentTime, row.ClientIp, row.Message, row.NumberOfEvents);
            }
        }
        if (snapshot.NewSecurityLogRefresh is DateTime securityLogRefresh)
            lastSecurityLogRefresh = securityLogRefresh;
        LastLogId = snapshot.ReplaceSecurityLog
            ? snapshot.MaxLogId
            : Math.Max(LastLogId, snapshot.MaxLogId);
        if (snapshot.NewLockUpdate is DateTime lockUpdate)
        {
            LastLockUpdate = lockUpdate;
            PanelCurrentLocks.Clear();
            foreach (AdminLockRow row in snapshot.Locks)
                PanelCurrentLocks.Add(row.Id, Properties.Resources.logIcon_softLock, LockStatusAdapter.GetLockStatusName(row.Status), row.ClientIp, row.DisplayName, row.LockDate, row.UnlockDate, row.Status);
        }
        if (snapshot.NewDashboardRefresh is DateTime dashboardRefresh)
        {
            lastDashboardRefresh = dashboardRefresh;
            if (snapshot.NewDashboardLogId is int dashboardLogId)
            {
                lastDashboardLogId = dashboardLogId;
            }
        }
        if (snapshot.FailedLoginStatistics is FailedLoginStatisticsSnapshot failedLogins &&
            snapshot.AgentLockStatistics is IReadOnlyDictionary<Guid, AgentLockStatistics> agentLocks)
        {
            Dashboard.SetUnsuccessfulLogins(failedLogins.Total);
            Dashboard.SetAgentStatistics(failedLogins.AttemptsByAgent, agentLocks);
        }
        if (snapshot.SoftLocks is int soft && snapshot.HardLocks is int hard)
        {
            PanelCurrentLocks.SetSoftLocks(soft);
            PanelCurrentLocks.SetHardLocks(hard);
            Dashboard.SetSoftLocks(soft);
            Dashboard.SetHardLocks(hard);
        }
        if (snapshot.CrossAgentAlerts is int alerts)
            Dashboard.SetCrossAgentAlerts(alerts);
    }

    private enum AdminRefreshMode
    { /// <summary>
      /// 定義 None 列舉值。
      /// </summary>
        None, /// <summary>
              /// 定義 SecurityLog 列舉值。
              /// </summary>
        SecurityLog, /// <summary>
                     /// 定義 CurrentLocks 列舉值。
                     /// </summary>
        CurrentLocks, /// <summary>
                      /// 定義 Dashboard 列舉值。
                      /// </summary>
        Dashboard
    }
    private sealed record AdminLogRow(int Id, int Action, string AgentId, DateTime IncidentTime, string ClientIp, string Message, int NumberOfEvents);
    private sealed record AdminLockRow(int Id, int Status, string ClientIp, string DisplayName, DateTime LockDate, DateTime UnlockDate);
    private sealed record AdminRefreshSnapshot(AdminRefreshMode Mode, IReadOnlyList<AdminLogRow> Logs, IReadOnlyList<AdminLockRow> Locks, int MaxLogId, DateTime? NewLockUpdate, DateTime? NewSecurityLogRefresh, bool ReplaceSecurityLog, FailedLoginStatisticsSnapshot? FailedLoginStatistics, IReadOnlyDictionary<Guid, AgentLockStatistics>? AgentLockStatistics, int? SoftLocks, int? HardLocks, int? CrossAgentAlerts, DateTime? NewDashboardRefresh = null, int? NewDashboardLogId = null);

    /// <summary>
    /// 取得或設定 LastLogId。
    /// </summary>
    public int LastLogId { get; set; }

    /// <summary>
    /// 取得或設定 LastLockUpdate。
    /// </summary>
    public DateTime LastLockUpdate { get; set; }
    /// <summary>
    /// 處理 invalidated 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void panelContent_Invalidated(object sender, InvalidateEventArgs e) => paintPanelTopBorder();

    /// <summary>
    /// 執行 paint panel top border 作業。
    /// </summary>
    void paintPanelTopBorder()
    {
        using Graphics graphics = Graphics.FromHwnd(panelContent.Handle);
        using Pen borderPen = new(Color.FromArgb(218, 226, 232), 1F);
        graphics.DrawLine(borderPen, Point.Empty, new Point(panelContent.ClientSize.Width, 0));
    }

    #region Form basics
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxCloseButton_Click(object sender, EventArgs e) => Close();
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseDown(object sender, MouseEventArgs e)
    {
        IsMoving = true;
        MoveStartPoint = new Point(e.X, e.Y);
    }

    /// <summary>
    /// 取得或設定 視窗是否正在拖曳移動中。
    /// </summary>
    public bool IsMoving { get; set; }
    /// <summary>
    /// 取得或設定 視窗拖曳起始座標點。
    /// </summary>
    public Point MoveStartPoint { get; set; }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseUp(object sender, MouseEventArgs e) => IsMoving = false;
    /// <summary>
    /// 處理 mouse move 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsMoving)
        {
            Location = new Point(Location.X + e.X - MoveStartPoint.X, Location.Y + e.Y - MoveStartPoint.Y);

        }
    }
    /// <summary>
    /// 處理首頁功能表項目點擊事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuHome_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuHome);
        ShowContentPanel(Dashboard);
        panelOnlineServices.Hide();
    }

    /// <summary>
    /// 切換內容區域顯示之面板，隱藏非使用中面板並將指定面板置於最上層。
    /// </summary>
    /// <param name="activePanel">欲顯示之主要內容面板控制項。</param>
    public void ShowContentPanel(Control activePanel)
    {
        foreach (Control c in panelContent.Controls)
        {
            if (c != panelOnlineServices)
            {
                c.Visible = (c == activePanel);
            }
        }
        activePanel.BringToFront();
    }

    /// <summary>
    /// 處理 FormClosing 事件，確保關閉應用程式前自動持久化儲存尚未存檔的 Agent 變更。
    /// </summary>
    /// <param name="e">FormClosingEventArgs 物件。</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        logReader?.Stop();
        timerRefreshServiceStatus?.Stop();
        _panelAgentConfiguration?.FlushUnsavedChanges();
        base.OnFormClosing(e);
    }

    /// <summary>
    /// 執行 show menu 作業。
    /// </summary>
    /// <param name="newMenu">new menu 的值。</param>
    private void ShowMenu(SmartLabel newMenu)
    {
        _panelAgentConfiguration?.FlushUnsavedChanges();
        //if (newMenu == CurrentMenu) return;
        if (CurrentMenu != null && newMenu != CurrentMenu)
        {
            CurrentMenu.Selected = false;
        }
        newMenu.Selected = true;
        CurrentMenu = newMenu;
        panelMenu.Invalidate();
        paintPanelTopBorder();

    }

    /// <summary>
    /// 取得或設定 CurrentMenu。
    /// </summary>
    public SmartLabel? CurrentMenu { get; set; }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuSecurityLog_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuSecurityLog);
        ShowContentPanel(PanelSecurityLog);
        panelOnlineServices.Hide();

    }
    /// <summary>
    /// 處理系統日誌功能表項目點擊事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuSystemLog_Click(object? sender, EventArgs e)
    {
        ShowMenu(labelMenuSystemLog);
        ShowContentPanel(PanelSystemOperationsLog);
        panelOnlineServices.Hide();
        _ = PanelSystemOperationsLog.LoadLogsAsync();
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuAgents_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuAgents);
        ShowContentPanel(PanelAgentConfiguration);
        panelOnlineServices.Hide();
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuSettings_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuSettings);
        ShowContentPanel(PanelApplicationSettings);
        panelOnlineServices.Hide();
    }


    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Close();
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox1_Click(object sender, EventArgs e) => pictureBox1.ContextMenuStrip?.Show(PointToScreen(new Point(pictureBox1.Location.X, pictureBox1.Location.Y + pictureBox1.Height)));
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxHelpButon_Click(object sender, EventArgs e)
    {
        bool isEnglish = LanguageManager.Instance.CurrentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        string[] candidatePaths = isEnglish
            ?
            [
                System.IO.Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.en-US.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "USER-GUIDE.en-US.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.zh-TW.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "USER-GUIDE.zh-TW.html")
            ]
            :
            [
                System.IO.Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.zh-TW.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "USER-GUIDE.zh-TW.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.en-US.html"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "USER-GUIDE.en-US.html")
            ];

        foreach (string path in candidatePaths)
        {
            if (System.IO.File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/ritai-kgm10179/IDDSCommunity#readme") { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(Strings.Get("Documentation is available in README.md."), Strings.AppTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxMinimizeButton_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxMaximizeButton_Click(object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Maximized)
        {
            MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
            WindowState = FormWindowState.Maximized;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_scale;
        }
        else
        {
            WindowState = FormWindowState.Normal;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_maximize;
        }
    }

    /// <summary>
    /// 處理 mouse enter 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseEnter(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonHighlight; }
    /// <summary>
    /// 處理 mouse leave 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseLeave(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseDown(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonPress; }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseUp(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelUnsuccessfulLogins_Click(object sender, EventArgs e)
    {

    }
    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void IddsAdmin_Load(object? sender, EventArgs e) =>

        //@DEMO: List demo agent
        InitAdmin();
    /// <summary>
    /// 執行 init agent settings 作業。
    /// </summary>
    public void InitAgentSettings()
    {
        Dashboard.ClearAgents();
        PanelAgentConfiguration.ClearSecurityAgents();
        SecurityAgents.Instance.SortAgents();
        foreach (SecurityAgent agent in SecurityAgents.Instance)
        {
            Dashboard.AddAgent(agent);
            PanelAgentConfiguration.LoadSecurityAgent(agent);
        }
    }
    /// <summary>
    /// 執行 init admin 作業。
    /// </summary>
    public void InitAdmin()
    {
        if (IsInitialized) return;
        InitAgentSettings();
        //SecurityAgent agentX = new SecurityAgent("FTP Security Agent", 1563, 18, 230, global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.Protection_Icon_32);

        //Dashboard.AddAgent(agentX);

        labelMenuHome_Click(labelMenuHome, EventArgs.Empty);

        //CurrentMenu = labelMenuHome;
        paintPanelTopBorder();

        // Invalidate(false);
        try
        {
            serviceController = new System.ServiceProcess.ServiceController(ServiceName);
            System.ServiceProcess.ServiceControllerStatus? initialStatus = ReadServiceStatus();
            IsServiceRunning = initialStatus == System.ServiceProcess.ServiceControllerStatus.Running;
            ApplyServiceStatus(initialStatus);
        }
        catch (Exception ex)
        {
            MarkServiceUnavailable(serviceController, ex);
        }
        if (serviceController is null)
            ApplyServiceStatus(null);
        logReader = new Timer
        {
            Interval = 1000
        };
        logReader.Tick += new EventHandler(logReader_Tick);
        logReader.Enabled = true;
        logReader.Start();

        timerRefreshServiceStatus = new Timer
        {
            Interval = 3000
        };
        timerRefreshServiceStatus.Tick += new EventHandler(serviceReader_Tick);
        timerRefreshServiceStatus.Start();

        eventLogIDDSCommunity = new EventLog(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME)
        {
            Source = Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE
        };

        ShowMenu(labelMenuHome);
        ShowContentPanel(Dashboard);
        IsInitialized = true;

    }

    /// <summary>
    /// 取得或設定 IsInitialized。
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Writes entry.
    /// </summary>
    /// <param name="text">text 的值。</param>
    /// <param name="type">type 的值。</param>
    /// <param name="eventId">event id 的值。</param>
    /// <param name="category">category 的值。</param>
    internal void WriteEntry(string text, EventLogEntryType type, int eventId, short category) => eventLogIDDSCommunity?.WriteEntry(text, type, eventId, category);
    /// <summary>
    /// 執行 resize form 作業。
    /// </summary>
    /// <param name="mouseLocation">mouse location 的值。</param>
    private void resizeForm(Point mouseLocation)
    {
        int deltaX = resizeStartLocation.X - mouseLocation.X;
        int deltaY = resizeStartLocation.Y - mouseLocation.Y;
        if ((resizeDirection & ResizeDirection.Left) == ResizeDirection.Left)
        {
            Left += -deltaX;
            Width += deltaX;
        }
        if ((resizeDirection & ResizeDirection.Right) == ResizeDirection.Right)
        {
            Width -= deltaX;
        }
        if ((resizeDirection & ResizeDirection.Top) == ResizeDirection.Top)
        {
            Height += deltaY;
            Top += -deltaY;
        }
        if ((resizeDirection & ResizeDirection.Bottom) == ResizeDirection.Bottom)
        {
            Height -= deltaY;
        }

    }

    ResizeDirection resizeDirection = ResizeDirection.None;
    bool isResizing = false;
    Point resizeStartLocation = new(0, 0);

    enum ResizeDirection
    {
        /// <summary>
        /// 定義 None 列舉值。
        /// </summary>
        None = 0,
        /// <summary>
        /// 定義 Top 列舉值。
        /// </summary>
        Top = 1,
        /// <summary>
        /// 定義 Right 列舉值。
        /// </summary>
        Right = 2,
        /// <summary>
        /// 定義 Bottom 列舉值。
        /// </summary>
        Bottom = 4,
        /// <summary>
        /// 定義 Left 列舉值。
        /// </summary>
        Left = 8
    }


    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderN_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Top;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderS_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderNE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Top;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderSE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderSW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderNW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Top;
        enterResizeMode(e.Location);
    }
    /// <summary>
    /// 執行 enter resize mode 作業。
    /// </summary>
    /// <param name="currentLocation">current location 的值。</param>
    private void enterResizeMode(Point currentLocation)
    {
        resizeStartLocation = currentLocation;
        isResizing = true;
        //this.SuspendLayout();
    }
    /// <summary>
    /// 處理 mouse move 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void border_MouseMove(object sender, MouseEventArgs e)
    {
        if (isResizing)
        {
            resizeForm(e.Location);
        }
    }
    /// <summary>
    /// 處理 resize 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void IddsAdmin_Resize(object sender, EventArgs e)
    {

    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void border_MouseUp(object sender, MouseEventArgs e) => isResizing = false;//this.ResumeLayout();

    #endregion
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuOnline_Click(object sender, EventArgs e)
    {
        if (panelOnlineServices.Visible)
        {
            panelOnlineServices.Hide();
        }
        else
        {
            panelOnlineServices.Dock = DockStyle.Left;
            panelOnlineServices.Show();
        }
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void labelMenuCurrentLocks_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuCurrentLocks);
        ShowContentPanel(PanelCurrentLocks);
        panelOnlineServices.Hide();
    }

    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void actionMenu_MouseDown(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X + 1, c.Location.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void actionMenu_MouseUp(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X - 1, c.Location.Y - 1);
    }

    /// <summary>
    /// 處理 paint 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelContent_Paint(object? sender, PaintEventArgs e) => paintPanelTopBorder();
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void pictureBoxStartService_Click(object sender, EventArgs e) => await ChangeServiceStateAsync(start: true);
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void pictureBoxStopService_Click(object sender, EventArgs e) => await ChangeServiceStateAsync(start: false);
    /// <summary>
    /// Confirms and performs installation or removal of the Windows service with on-demand elevation.
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void buttonManageService_Click(object sender, EventArgs e)
    {
        bool install = serviceController is null;
        string prompt = Strings.Get(install
            ? "Install the IDDSCommunity protection service on this computer?"
            : "Uninstall the IDDSCommunity protection service? Active protection will stop.");
        if (MessageBox.Show(this, prompt, Strings.Get("Confirm service change"), MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        buttonManageService.Enabled = false;
        bool gateEntered = false;
        try
        {
            await serviceOperationGate.WaitAsync(uiRefreshCancellation.Token).ConfigureAwait(false);
            gateEntered = true;
            install = serviceController is null;
            await ElevatedServiceCommand.RunElevatedAsync(ServiceName, install ? "install" : "uninstall", uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ResetServiceController(), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Trace.TraceError("Service installation state change failed: {0}", exception);
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() => MessageBox.Show(this, Strings.Get("The service change could not be completed."),
                    Strings.Get("Service operation failed"), MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
        finally
        {
            if (gateEntered) serviceOperationGate.Release();
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() => buttonManageService.Enabled = true);
        }
    }

    private void ResetServiceController()
    {
        serviceController?.Dispose();
        serviceController = null;
        ServiceError = false;
        try
        {
            serviceController = new System.ServiceProcess.ServiceController(ServiceName);
            System.ServiceProcess.ServiceControllerStatus? status = ReadServiceStatus();
            ApplyServiceStatus(status);
            timerRefreshServiceStatus?.Start();
        }
        catch (Exception exception)
        {
            MarkServiceUnavailable(serviceController, exception);
            ApplyServiceStatus(null);
            timerRefreshServiceStatus?.Start();
        }
    }
    /// <summary>
    /// Changes the Windows service state without blocking the WinForms message loop.
    /// </summary>
    /// <param name="start"><see langword="true"/> to start the service; <see langword="false"/> to stop it.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    private async Task ChangeServiceStateAsync(bool start)
    {
        bool gateEntered = false;
        try
        {
            await serviceOperationGate.WaitAsync(uiRefreshCancellation.Token);
            gateEntered = true;
            smartLabelServiceStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            string pending = Strings.Get(start ? "Starting service..." : "Stopping service...");
            smartLabelServiceStatus.Text = StatusDot + " " + pending;
            smartLabelServiceStatus.ForeColor = Color.FromArgb(245, 158, 11);
            System.ServiceProcess.ServiceController? controller = serviceController;
            if (controller is null) return;
            await ElevatedServiceCommand.RunElevatedAsync(ServiceName, start ? "start" : "stop", uiRefreshCancellation.Token).ConfigureAwait(false);
            System.ServiceProcess.ServiceControllerStatus? status = await Task.Run(() =>
            {
                controller.Refresh();
                return (System.ServiceProcess.ServiceControllerStatus?)controller.Status;
            }, uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyServiceStatus(status), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            System.ServiceProcess.ServiceController? controller = serviceController;
            MarkServiceUnavailable(controller, ex);
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() => ApplyServiceStatus(null));
        }
        finally
        {
            if (gateEntered) serviceOperationGate.Release();
        }
    }
    /// <summary>
    /// Detaches an unusable service controller, records diagnostics, and exposes the unavailable UI state.
    /// </summary>
    /// <param name="controller">The controller that failed, if one was created.</param>
    /// <param name="exception">The service-control failure.</param>
    private void MarkServiceUnavailable(System.ServiceProcess.ServiceController? controller, Exception exception)
    {
        if (ReferenceEquals(serviceController, controller))
            serviceController = null;
        controller?.Dispose();
        ServiceError = true;
        try
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Admin", exception.Message, EventLogEntryType.Error);
        }
        catch (Exception logException)
        {
            Trace.TraceError("Service state read failed: {0}{1}Event Log write failed: {2}", exception, Environment.NewLine, logException);
            _ = RollingDiagnosticLog.Write("Admin-ServiceStatus", "Service status and Event Log write failed", exception);
        }
    }






}

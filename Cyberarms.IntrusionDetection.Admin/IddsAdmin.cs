using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class IddsAdmin : Form
{
    private const string ServiceName = "Cyberarms Intrusion Detection Service";
    readonly Color buttonHighlight = Color.FromArgb(205, 230, 247);
    readonly Color buttonPress = Color.FromArgb(105, 130, 147);
    readonly Color buttonNormal = Color.FromKnownColor(KnownColor.Window);
    Timer? logReader;
    Timer? timerRefreshServiceStatus;
    CyberarmsSecurityLog? _panelSecurityLog;
    CyberarmsCurrentLocks? _panelCurrentLocks;
    CyberarmsDashboard? _dashboard;
    CyberarmsAgentConfiguration? _panelAgentConfiguration;
    CyberarmsApplicationSettings? _panelApplicationSettings;

    System.ServiceProcess.ServiceController? serviceController;
    private EventLog? eventLogCyberarms;
    private readonly System.Threading.CancellationTokenSource uiRefreshCancellation = new();
    private int serviceRefreshActive;
    /// <summary>
    /// Initializes a new instance of the <see cref="IddsAdmin"/> class.
    /// </summary>

    public IddsAdmin()
    {
        InitializeComponent();
        Text = Strings.Format("Cyberarms Intrusion Detection - Version {0}", Application.ProductVersion);
        labelFormText.Text = Text;

        //            panelContent.Invalidated += new InvalidateEventHandler(panelContent_Invalidated);
        panelContent.Paint += new PaintEventHandler(panelContent_Paint);

        Load += new EventHandler(IddsAdmin_Load);
    }

    /// <summary>
    /// Cancels pending background snapshots before WinForms destroys control handles.
    /// </summary>
    /// <param name="e">The form-close event data.</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        uiRefreshCancellation.Cancel();
        base.OnFormClosed(e);
    }

    private static IddsAdmin? _instance;
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


    public CyberarmsApplicationSettings PanelApplicationSettings
    {
        get
        {
            if (_panelApplicationSettings == null)
            {
                _panelApplicationSettings = new CyberarmsApplicationSettings
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
    /// Handles the configuration changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    async void _panelApplicationSettings_ConfigurationChanged(object? sender, EventArgs e) => await RestartServiceAsync();

    public CyberarmsAgentConfiguration PanelAgentConfiguration
    {
        get
        {
            if (_panelAgentConfiguration == null)
            {
                _panelAgentConfiguration = new CyberarmsAgentConfiguration
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
    /// Handles the agent settings changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    async void _panelAgentConfiguration_AgentSettingsChanged(object? sender, EventArgs e) => await RestartServiceAsync();

    /// <summary>
    /// Handles the plugins changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _panelAgentConfiguration_PluginsChanged(object? sender, EventArgs e) => InitAgentSettings();//RestartService();

    /// <summary>
    /// Executes the restart service operation.
    /// </summary>

    public async Task RestartServiceAsync()
    {
        System.ServiceProcess.ServiceController? controller = serviceController;
        if (controller is null)
        {
            ApplyServiceStatus(null);
            return;
        }
        try
        {
            System.ServiceProcess.ServiceControllerStatus status = await Task.Run(() =>
            {
                controller.Refresh();
                if (controller.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    controller.Stop();
                    controller.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    controller.Start();
                    controller.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                controller.Refresh();
                return controller.Status;
            }, uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyServiceStatus(status), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            MarkServiceUnavailable(controller, ex);
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() => ApplyServiceStatus(null));
        }
    }

    public CyberarmsSecurityLog PanelSecurityLog
    {
        get
        {
            if (_panelSecurityLog == null)
            {
                _panelSecurityLog = new CyberarmsSecurityLog
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelSecurityLog);
                IsUpdating = true;

                IDataReader rdr = IntrusionLog.ReadIntervalGrouped(new TimeSpan(24, 0, 0));
                int maxLogId = LastLogId;
                while (rdr.Read())
                {
                    int action = Shared.Db.DbValueConverter.ToInt(rdr["Action"]);
                    string agentId = Shared.Db.DbValueConverter.ToString(rdr["AgentId"]);
                    PanelSecurityLog.FillLogEntry(Shared.Db.DbValueConverter.ToInt(rdr["MaxId"]),
                            action,
                            agentId,
                            IntrusionLog.GetStatusIcon(action),
                            IntrusionLog.GetStatusClass(action), Shared.Db.DbValueConverter.ToDateTime(rdr["LatestEvent"]),
                            Shared.Db.DbValueConverter.ToString(rdr["ClientIP"]),
                            GetLogMessage(agentId, action),
                            Shared.Db.DbValueConverter.ToInt(rdr["NumberOfEvents"]));
                    if (Convert.ToInt32(rdr["MaxId"]) > maxLogId) maxLogId = Convert.ToInt32(rdr["MaxId"]);
                }
                if (maxLogId == 0)
                {
                    LastLogId = IntrusionLog.GetLastLogId();
                }
                foreach (SecurityAgent agent in SecurityAgents.Instance)
                {
                    _panelSecurityLog.AddAgent(agent);
                }
                rdr.Close();
                if (maxLogId > LastLogId) LastLogId = maxLogId;
                IsUpdating = false;
            }
            return _panelSecurityLog;
        }
    }

    /// <summary>
    /// Executes the fill log operation.
    /// </summary>
    /// <param name="rdr">The rdr value.</param>

    private static void FillLog(IDataReader rdr)
    {
    }

    public CyberarmsCurrentLocks PanelCurrentLocks
    {
        get
        {
            if (_panelCurrentLocks == null)
            {
                _panelCurrentLocks = new CyberarmsCurrentLocks
                {
                    Dock = DockStyle.Fill
                };
                panelContent.Controls.Add(_panelCurrentLocks);
            }
            return _panelCurrentLocks;
        }
    }

    public CyberarmsDashboard Dashboard
    {
        get
        {
            if (_dashboard == null)
            {
                _dashboard = new CyberarmsDashboard
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
    /// Handles the security agent configuration request event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _dashboard_SecurityAgentConfigurationRequest(object? sender, EventArgs e)
    {
        if (sender != null && sender is SecurityAgent)
        {
            ShowMenu(labelMenuAgents);
            if (sender is SecurityAgent agent)
                PanelAgentConfiguration.ShowAgentConfig(agent);
            PanelAgentConfiguration.BringToFront();
            panelOnlineServices.Hide();
        }
    }

    public bool IsServiceRunning { get; set; }

    /// <summary>
    /// Handles the tick event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    async void serviceReader_Tick(object? sender, EventArgs e)
    {
        if (System.Threading.Interlocked.Exchange(ref serviceRefreshActive, 1) != 0)
            return;
        try
        {
            System.ServiceProcess.ServiceControllerStatus? status = await Task.Run(ReadServiceStatus, uiRefreshCancellation.Token).ConfigureAwait(false);
            if (!IsDisposed)
                await this.InvokeAsync(() =>
                {
                    ApplyServiceStatus(status);
                    if (status is null)
                        timerRefreshServiceStatus?.Stop();
                }, uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception)
        {
            ServiceError = true;
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref serviceRefreshActive, 0);
        }
    }


    public bool ServiceError { get; set; }

    /// <summary>
    /// Executes the refresh service status operation.
    /// </summary>

    public void RefreshServiceStatus()
    {
        ApplyServiceStatus(ReadServiceStatus());
    }

    /// <summary>
    /// Reads the current Windows service status without accessing UI controls.
    /// </summary>
    /// <returns>The service status, or <see langword="null"/> when no controller is available.</returns>
    private System.ServiceProcess.ServiceControllerStatus? ReadServiceStatus()
    {
        System.ServiceProcess.ServiceController? controller = serviceController;
        if (controller is null)
            return null;
        if (WindowsServiceStatusReader.TryRead(() =>
        {
            controller.Refresh();
            return controller.Status;
        }, out System.ServiceProcess.ServiceControllerStatus status, out Exception? failure))
            return status;
        if (failure is not null)
        {
            MarkServiceUnavailable(controller, failure);
        }
        return null;
    }

    /// <summary>
    /// Applies one service-status snapshot on the UI thread.
    /// </summary>
    /// <param name="status">The status read by the background operation.</param>
    private void ApplyServiceStatus(System.ServiceProcess.ServiceControllerStatus? status)
    {
        if (ServiceError)
        {
            smartLabelServiceStatus.Text = Strings.Get("Service not found!");
            smartLabelServiceStatus.ForeColor = Color.FromArgb(225, 50, 50);
            return;
        }
        try
        {
            if (status == System.ServiceProcess.ServiceControllerStatus.Running && !IsServiceRunning)
            {
                IsServiceRunning = true;
                pictureBoxStartService.Image = Properties.Resources.service_controller_start_deactivated;
                pictureBoxStartService.Enabled = false;
                pictureBoxStopService.Image = Properties.Resources.service_controller_stop;
                pictureBoxStopService.Enabled = true;
                smartLabelServiceStatus.Text = Strings.Get("Service is running");
                smartLabelServiceStatus.ForeColor = Color.FromArgb(0, 159, 227);
            }
            else if (status == System.ServiceProcess.ServiceControllerStatus.Stopped && IsServiceRunning)
            {
                IsServiceRunning = false;
                pictureBoxStartService.Image = Properties.Resources.service_controller_start;
                pictureBoxStartService.Enabled = true;
                pictureBoxStopService.Image = Properties.Resources.service_controller_stop_deactivated;
                pictureBoxStopService.Enabled = false;
                smartLabelServiceStatus.Text = Strings.Get("Service is stopped");
                smartLabelServiceStatus.ForeColor = Color.FromArgb(225, 50, 50);
            }
        }
        catch
        {
            ServiceError = true;
        }

    }

    public bool IsUpdating { get; set; }

    /// <summary>
    /// Gets log message.
    /// </summary>
    /// <param name="agentId">The agent id value.</param>
    /// <param name="action">The action value.</param>
    /// <returns>The get log message result.</returns>

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
    /// Handles the tick event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
        try
        {
            AdminRefreshSnapshot snapshot = await Task.Run(() => LoadAdminSnapshot(mode, lastLogId, lastLockUpdate), uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyAdminSnapshot(snapshot), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            try
            {
                EventLog.WriteEntry("Cyberarms.IntrusionDetection.Admin", ex.Message, EventLogEntryType.Error);
            }
            catch (Exception)
            {
                // UI refresh failures must not terminate the WinForms message loop.
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
                        IsInitialized = true;
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
    /// <returns>The background-loaded administration snapshot.</returns>
    private static AdminRefreshSnapshot LoadAdminSnapshot(AdminRefreshMode mode, int lastLogId, DateTime lastLockUpdate)
    {
        List<AdminLogRow> logs = [];
        List<AdminLockRow> locks = [];
        int maxLogId = lastLogId;
        DateTime? newLockUpdate = null;
        int? unsuccessfulLogins = null;
        int? softLocks = null;
        int? hardLocks = null;
        if (mode == AdminRefreshMode.SecurityLog && IntrusionLog.HasUpdates(lastLogId))
        {
            using IDataReader reader = IntrusionLog.ReadDifferential(lastLogId);
            while (reader.Read())
            {
                int id = Shared.Db.DbValueConverter.ToInt(reader["Id"]);
                int action = Shared.Db.DbValueConverter.ToInt(reader["Action"]);
                string agentId = Shared.Db.DbValueConverter.ToString(reader["AgentId"]);
                logs.Add(new AdminLogRow(id, action, agentId, Shared.Db.DbValueConverter.ToDateTime(reader["IncidentTime"]), Shared.Db.DbValueConverter.ToString(reader["ClientIP"]), GetLogMessage(agentId, action)));
                maxLogId = Math.Max(maxLogId, id);
            }
        }
        if (mode == AdminRefreshMode.CurrentLocks && Locks.HasUpdates(lastLockUpdate))
        {
            newLockUpdate = DateTime.Now;
            using IDataReader reader = Locks.ReadLocks();
            while (reader.Read())
            {
                DateTime.TryParse(reader["LockDate"].ToString(), out DateTime lockDate);
                DateTime.TryParse(reader["UnlockDate"].ToString(), out DateTime unlockDate);
                int status = Shared.Db.DbValueConverter.ToInt(reader["Status"]);
                locks.Add(new AdminLockRow(Shared.Db.DbValueConverter.ToInt(reader["LockId"]), status, Shared.Db.DbValueConverter.ToString(reader["ClientIp"]), Shared.Db.DbValueConverter.ToString(reader["DisplayName"]), lockDate, unlockDate));
            }
        }
        if (mode == AdminRefreshMode.Dashboard)
        {
            unsuccessfulLogins = Locks.ReadUnsuccessfulLoginAttempts(DateTime.Now.AddDays(-30));
            foreach (SecurityAgent agent in SecurityAgents.Instance)
                agent.UpdateStatistics();
        }
        if (mode is AdminRefreshMode.Dashboard or AdminRefreshMode.CurrentLocks)
        {
            softLocks = Locks.ReadCurrentSoftLocks();
            hardLocks = Locks.ReadCurrentHardLocks();
        }
        return new AdminRefreshSnapshot(mode, logs, locks, maxLogId, newLockUpdate, unsuccessfulLogins, softLocks, hardLocks);
    }

    /// <summary>
    /// Applies a background-loaded administration snapshot on the UI thread.
    /// </summary>
    /// <param name="snapshot">The immutable values to display.</param>
    private void ApplyAdminSnapshot(AdminRefreshSnapshot snapshot)
    {
        foreach (AdminLogRow row in snapshot.Logs)
            PanelSecurityLog.AddLogEntry(row.Id, row.Action, row.AgentId, IntrusionLog.GetStatusIcon(row.Action), IntrusionLog.GetStatusClass(row.Action), row.IncidentTime, row.ClientIp, row.Message);
        LastLogId = Math.Max(LastLogId, snapshot.MaxLogId);
        if (snapshot.NewLockUpdate is DateTime lockUpdate)
        {
            LastLockUpdate = lockUpdate;
            PanelCurrentLocks.Clear();
            foreach (AdminLockRow row in snapshot.Locks)
                PanelCurrentLocks.Add(row.Id, Properties.Resources.logIcon_softLock, LockStatusAdapter.GetLockStatusName(row.Status), row.ClientIp, row.DisplayName, row.LockDate, row.UnlockDate, row.Status);
        }
        if (snapshot.UnsuccessfulLogins is int unsuccessful)
            Dashboard.SetUnsuccessfulLogins(unsuccessful);
        if (snapshot.SoftLocks is int soft && snapshot.HardLocks is int hard)
        {
            PanelCurrentLocks.SetSoftLocks(soft);
            PanelCurrentLocks.SetHardLocks(hard);
            Dashboard.SetSoftLocks(soft);
            Dashboard.SetHardLocks(hard);
        }
    }

    private enum AdminRefreshMode { None, SecurityLog, CurrentLocks, Dashboard }
    private sealed record AdminLogRow(int Id, int Action, string AgentId, DateTime IncidentTime, string ClientIp, string Message);
    private sealed record AdminLockRow(int Id, int Status, string ClientIp, string DisplayName, DateTime LockDate, DateTime UnlockDate);
    private sealed record AdminRefreshSnapshot(AdminRefreshMode Mode, IReadOnlyList<AdminLogRow> Logs, IReadOnlyList<AdminLockRow> Locks, int MaxLogId, DateTime? NewLockUpdate, int? UnsuccessfulLogins, int? SoftLocks, int? HardLocks);

    public int LastLogId { get; set; }

    public DateTime LastLockUpdate { get; set; }

    /// <summary>
    /// Handles the invalidated event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void panelContent_Invalidated(object sender, InvalidateEventArgs e) => paintPanelTopBorder();


    /// <summary>
    /// Executes the paint panel top border operation.
    /// </summary>

    void paintPanelTopBorder()
    {
        if (CurrentMenu != null)
        {
            var g = Graphics.FromHwnd(panelContent.Handle);
            Pen borderPen = new(Color.FromArgb(190, 190, 190), 1);
            Pen backgroundPen = new(panelContent.BackColor, 1);
            System.Drawing.Drawing2D.GraphicsPath path = new();
            g.DrawLine(borderPen, new Point(0, 0), new Point(CurrentMenu.Location.X + 1, 0));
            g.DrawLine(backgroundPen, new Point(CurrentMenu.Location.X + 1, 0), new Point(CurrentMenu.Location.X + CurrentMenu.Width - 1, 0));
            g.DrawLine(borderPen, new Point(CurrentMenu.Location.X + CurrentMenu.Width - 1, 0), new Point(Width, 0));
        }
    }

    #region Form basics
    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxCloseButton_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseDown(object sender, MouseEventArgs e)
    {
        IsMoving = true;
        MoveStartPoint = new Point(e.X, e.Y);
    }

    public bool IsMoving { get; set; }
    public Point MoveStartPoint { get; set; }
    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseUp(object sender, MouseEventArgs e) => IsMoving = false;

    /// <summary>
    /// Handles the mouse move event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsMoving)
        {
            Location = new Point(Location.X + e.X - MoveStartPoint.X, Location.Y + e.Y - MoveStartPoint.Y);

        }
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void labelMenuHome_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuHome);
        Dashboard.BringToFront();
        panelOnlineServices.Hide();
    }


    /// <summary>
    /// Executes the show menu operation.
    /// </summary>
    /// <param name="newMenu">The new menu value.</param>

    private void ShowMenu(SmartLabel newMenu)
    {
        //if (newMenu == CurrentMenu) return;
        if (CurrentMenu != null && newMenu != CurrentMenu)
        {
            CurrentMenu.Selected = false;
        }
        newMenu.Selected = true;
        CurrentMenu = newMenu;
        //paintPanelTopBorder(CurrentMenu);
        paintPanelTopBorder();

    }

    public SmartLabel? CurrentMenu { get; set; }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void labelMenuSecurityLog_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuSecurityLog);
        //panelSecurityLog.BringToFront();
        PanelSecurityLog.BringToFront();
        panelOnlineServices.Hide();

    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void labelMenuAgents_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuAgents);
        PanelAgentConfiguration.BringToFront();
        panelOnlineServices.Hide();
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void labelMenuSettings_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuSettings);
        PanelApplicationSettings.BringToFront();
        panelOnlineServices.Hide();
    }



    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBox1_Click(object sender, EventArgs e) => pictureBox1.ContextMenuStrip?.Show(PointToScreen(new Point(pictureBox1.Location.X, pictureBox1.Location.Y + pictureBox1.Height)));

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxHelpButon_Click(object sender, EventArgs e)
    {
        ProcessStartInfo sInfo = new("http://cyberarms.net/iddshelp/");
        Process.Start(sInfo);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxMinimizeButton_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxMaximizeButton_Click(object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Maximized)
        {
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
    /// Handles the mouse enter event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseEnter(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonHighlight; }

    /// <summary>
    /// Handles the mouse leave event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseLeave(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseDown(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonPress; }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseUp(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelUnsuccessfulLogins_Click(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IddsAdmin_Load(object? sender, EventArgs e) =>

        //@DEMO: List demo agent
        InitAdmin();

    /// <summary>
    /// Executes the init agent settings operation.
    /// </summary>

    public void InitAgentSettings()
    {
        Dashboard.ClearAgents();
        PanelAgentConfiguration.ClearSecurityAgents();
        foreach (SecurityAgent agent in SecurityAgents.Instance)
        {
            Dashboard.AddAgent(agent);
            PanelAgentConfiguration.LoadSecurityAgent(agent);
        }
    }

    /// <summary>
    /// Executes the init admin operation.
    /// </summary>

    public void InitAdmin()
    {
        if (IsInitialized) return;
        InitAgentSettings();
        //SecurityAgent agentX = new SecurityAgent("FTP Security Agent", 1563, 18, 230, global::Cyberarms.IntrusionDetection.Admin.Properties.Resources.Paladin_Icon_32);

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
        {
            using GenericErrorDialog errdlg = new(
                Strings.Get("Error starting application"),
                Strings.Get("The Cyberarms service is not installed or is unavailable. Install or repair the service, then restart the application."),
                false);
            errdlg.ShowDialog();
        }
        logReader = new Timer
        {
            Interval = 1000
        };
        logReader.Tick += new EventHandler(logReader_Tick);
        logReader.Enabled = true;
        logReader.Start();

        timerRefreshServiceStatus = new Timer
        {
            Interval = 1000
        };
        timerRefreshServiceStatus.Tick += new EventHandler(serviceReader_Tick);
        timerRefreshServiceStatus.Enabled = serviceController is not null;
        if (timerRefreshServiceStatus.Enabled)
            timerRefreshServiceStatus.Start();

        eventLogCyberarms = new EventLog("Cyberarms")
        {
            Source = "Cyberarms Intrusion Detection"
        };

        ShowMenu(labelMenuHome);
        Dashboard.BringToFront();

    }

    public bool IsInitialized { get; set; }


    /// <summary>
    /// Writes entry.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="eventId">The event id value.</param>
    /// <param name="category">The category value.</param>

    internal void WriteEntry(string text, EventLogEntryType type, int eventId, short category) => eventLogCyberarms?.WriteEntry(text, type, eventId, category);

    /// <summary>
    /// Executes the resize form operation.
    /// </summary>
    /// <param name="mouseLocation">The mouse location value.</param>

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
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8
    }



    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderN_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Top;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderS_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderNE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Top;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderSE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderSW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Bottom;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderNW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Top;
        enterResizeMode(e.Location);
    }

    /// <summary>
    /// Executes the enter resize mode operation.
    /// </summary>
    /// <param name="currentLocation">The current location value.</param>

    private void enterResizeMode(Point currentLocation)
    {
        resizeStartLocation = currentLocation;
        isResizing = true;
        //this.SuspendLayout();
    }

    /// <summary>
    /// Handles the mouse move event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void border_MouseMove(object sender, MouseEventArgs e)
    {
        if (isResizing)
        {
            resizeForm(e.Location);
        }
    }
    /// <summary>
    /// Handles the resize event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IddsAdmin_Resize(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void border_MouseUp(object sender, MouseEventArgs e) => isResizing = false;//this.ResumeLayout();

    #endregion

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void labelMenuCurrentLocks_Click(object sender, EventArgs e)
    {
        ShowMenu(labelMenuCurrentLocks);
        //panelCurrentLocks.BringToFront();
        PanelCurrentLocks.BringToFront();
        panelOnlineServices.Hide();
    }


    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void actionMenu_MouseDown(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X + 1, c.Location.Y + 1);
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void actionMenu_MouseUp(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X - 1, c.Location.Y - 1);
    }


    /// <summary>
    /// Handles the paint event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelContent_Paint(object? sender, PaintEventArgs e) => paintPanelTopBorder();

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private async void pictureBoxStartService_Click(object sender, EventArgs e) => await ChangeServiceStateAsync(start: true);

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private async void pictureBoxStopService_Click(object sender, EventArgs e) => await ChangeServiceStateAsync(start: false);

    /// <summary>
    /// Changes the Windows service state without blocking the WinForms message loop.
    /// </summary>
    /// <param name="start"><see langword="true"/> to start the service; <see langword="false"/> to stop it.</param>
    /// <returns>A task that completes after the requested state is observed or the operation fails.</returns>
    private async Task ChangeServiceStateAsync(bool start)
    {
        smartLabelServiceStatus.Text = Strings.Get(start ? "Starting service..." : "Stopping service...");
        smartLabelServiceStatus.ForeColor = Color.FromArgb(0x666666);
        System.ServiceProcess.ServiceController? controller = serviceController;
        if (controller is null)
            return;
        try
        {
            System.ServiceProcess.ServiceControllerStatus? status = await Task.Run(() =>
            {
                controller.Refresh();
                if (start && controller.Status is System.ServiceProcess.ServiceControllerStatus.Paused or System.ServiceProcess.ServiceControllerStatus.Stopped)
                {
                    controller.Start();
                    controller.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                else if (!start && controller.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    controller.Stop();
                    controller.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                controller.Refresh();
                return (System.ServiceProcess.ServiceControllerStatus?)controller.Status;
            }, uiRefreshCancellation.Token).ConfigureAwait(false);
            await this.InvokeAsync(() => ApplyServiceStatus(status), uiRefreshCancellation.Token);
        }
        catch (OperationCanceledException) when (uiRefreshCancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            MarkServiceUnavailable(controller, ex);
            if (!IsDisposed && IsHandleCreated)
                await this.InvokeAsync(() => ApplyServiceStatus(null));
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
            EventLog.WriteEntry("Cyberarms.IntrusionDetection.Admin", exception.Message, EventLogEntryType.Error);
        }
        catch (Exception)
        {
            // A missing Event Log source must not turn a recoverable service-state failure into a startup crash.
        }
    }






}

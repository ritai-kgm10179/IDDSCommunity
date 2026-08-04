using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;
using System.Diagnostics;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class IddsAdmin : Form
{
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
    /// <summary>
    /// Initializes a new instance of the <see cref="IddsAdmin"/> class.
    /// </summary>

    public IddsAdmin()
    {
        InitializeComponent();
        Text = "Cyberarms Intrusion Detection - Version " + Application.ProductVersion;
        labelFormText.Text = Text;

        //            panelContent.Invalidated += new InvalidateEventHandler(panelContent_Invalidated);
        panelContent.Paint += new PaintEventHandler(panelContent_Paint);

        Load += new EventHandler(IddsAdmin_Load);
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

    void _panelApplicationSettings_ConfigurationChanged(object? sender, EventArgs e) => RestartService();

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

    void _panelAgentConfiguration_AgentSettingsChanged(object? sender, EventArgs e) => RestartService();

    /// <summary>
    /// Handles the plugins changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _panelAgentConfiguration_PluginsChanged(object? sender, EventArgs e) => InitAgentSettings();//RestartService();

    /// <summary>
    /// Executes the restart service operation.
    /// </summary>

    public void RestartService()
    {
        if (serviceController != null && serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Running)
        {
            try
            {
                serviceController.Stop();
                while (serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Running ||
                    serviceController.Status == System.ServiceProcess.ServiceControllerStatus.StopPending)
                {
                    Application.DoEvents();
                }
                serviceController.Start();
            }
            catch { }
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

    void serviceReader_Tick(object? sender, EventArgs e) => RefreshServiceStatus();


    public bool ServiceError { get; set; }

    /// <summary>
    /// Executes the refresh service status operation.
    /// </summary>

    public void RefreshServiceStatus()
    {
        serviceController?.Refresh();
        if (ServiceError)
        {
            smartLabelServiceStatus.Text = Strings.Get("Service not found!");
            smartLabelServiceStatus.ForeColor = Color.FromArgb(225, 50, 50);
            return;
        }
        try
        {
            if (serviceController?.Status == System.ServiceProcess.ServiceControllerStatus.Running && !IsServiceRunning)
            {
                IsServiceRunning = true;
                pictureBoxStartService.Image = Properties.Resources.service_controller_start_deactivated;
                pictureBoxStartService.Enabled = false;
                pictureBoxStopService.Image = Properties.Resources.service_controller_stop;
                pictureBoxStopService.Enabled = true;
                smartLabelServiceStatus.Text = Strings.Get("Service is running");
                smartLabelServiceStatus.ForeColor = Color.FromArgb(0, 159, 227);
            }
            else if (serviceController?.Status == System.ServiceProcess.ServiceControllerStatus.Stopped && IsServiceRunning)
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

    void logReader_Tick(object? sender, EventArgs e)
    {
        DateTime metering = DateTime.Now;
        if (!IsUpdating && Database.Instance.IsConfigured)
        {
            IsUpdating = true;
            if (CurrentMenu == labelMenuSecurityLog && IntrusionLog.HasUpdates(LastLogId))
            {
                IDataReader rdr = IntrusionLog.ReadDifferential(LastLogId);
                int maxLogId = LastLogId;
                while (rdr.Read())
                {
                    int action = Shared.Db.DbValueConverter.ToInt(rdr["Action"]);
                    string agentId = Shared.Db.DbValueConverter.ToString(rdr["AgentId"]);
                    PanelSecurityLog.AddLogEntry(Shared.Db.DbValueConverter.ToInt(rdr["id"]), action,
                        agentId,
                        IntrusionLog.GetStatusIcon(action),
                        IntrusionLog.GetStatusClass(action), Shared.Db.DbValueConverter.ToDateTime(rdr["IncidentTime"]), Shared.Db.DbValueConverter.ToString(rdr["ClientIP"]),
                        GetLogMessage(agentId, action));
                    if (Convert.ToInt32(rdr["Id"]) > maxLogId) maxLogId = Convert.ToInt32(rdr["Id"]);
                }
                rdr.Close();
                rdr.Dispose();
                if (maxLogId > LastLogId) LastLogId = maxLogId;
            }

            if (CurrentMenu == labelMenuCurrentLocks && Locks.HasUpdates(LastLockUpdate))
            {
                LastLockUpdate = DateTime.Now;
                PanelCurrentLocks.Clear();
                IDataReader locksReader = Locks.ReadLocks();
                while (locksReader.Read())
                {
                    DateTime.TryParse(locksReader["LockDate"].ToString(), out DateTime lockDate);
                    DateTime.TryParse(locksReader["UnlockDate"].ToString(), out DateTime unlockDate);
                    PanelCurrentLocks.Add(Shared.Db.DbValueConverter.ToInt(locksReader["LockId"]), Properties.Resources.logIcon_softLock,
                        LockStatusAdapter.GetLockStatusName(Shared.Db.DbValueConverter.ToInt(locksReader["Status"])), Shared.Db.DbValueConverter.ToString(locksReader["ClientIp"]),
                        Shared.Db.DbValueConverter.ToString(locksReader["DisplayName"]),
                        lockDate, unlockDate, Shared.Db.DbValueConverter.ToInt(locksReader["Status"]));
                }
                locksReader.Close();
                locksReader.Dispose();

            }
            if (CurrentMenu == labelMenuHome)
            {
                Dashboard.SetUnsuccessfulLogins(Locks.ReadUnsuccessfulLoginAttempts(DateTime.Now.AddDays(-30)));
                foreach (SecurityAgent agent in SecurityAgents.Instance)
                {
                    agent.UpdateStatistics();
                }
            }
            if (CurrentMenu == labelMenuHome || CurrentMenu == labelMenuCurrentLocks)
            {
                int softLocks = Locks.ReadCurrentSoftLocks();
                int hardLocks = Locks.ReadCurrentHardLocks();
                PanelCurrentLocks.SetSoftLocks(softLocks);
                PanelCurrentLocks.SetHardLocks(hardLocks);
                Dashboard.SetHardLocks(hardLocks);
                Dashboard.SetSoftLocks(softLocks);

            }
            if (!IsInitialized || CurrentMenu == labelMenuHome || CurrentMenu == labelMenuSecurityLog)
            {
                // ?? 
            }
        }
        IsInitialized = true;
        IsUpdating = false;
        Debug.Print(DateTime.Now.Subtract(metering).TotalMilliseconds.ToString());
    }

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
            serviceController = new System.ServiceProcess.ServiceController("Cyberarms Intrusion Detection Service");
            IsServiceRunning = serviceController.Status != System.ServiceProcess.ServiceControllerStatus.Running;
        }
        catch (Exception ex)
        {
            GenericErrorDialog errdlg = new("Error starting application", "The service is not installed or installed correctly. Please uninstall Cyberarms IDDS and reinstall to fix the problem!", false);
            errdlg.ShowDialog();
            EventLog.WriteEntry("Cyberarms.IntrusionDetection.Admin", ex.Message);
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
        timerRefreshServiceStatus.Enabled = true;
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

    private void pictureBoxStartService_Click(object sender, EventArgs e) => StartService();

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxStopService_Click(object sender, EventArgs e) => StopService();

    /// <summary>
    /// Starts service.
    /// </summary>

    private void StartService()
    {
        smartLabelServiceStatus.Text = Strings.Get("Starting service...");
        smartLabelServiceStatus.ForeColor = Color.FromArgb(0x666666);
        if (serviceController is not null && (serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Paused ||
            serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Stopped))
        {
            serviceController.Start();
        }
        RefreshServiceStatus();
    }

    /// <summary>
    /// Stops service.
    /// </summary>

    private void StopService()
    {
        smartLabelServiceStatus.Text = Strings.Get("Stopping service...");
        smartLabelServiceStatus.ForeColor = Color.FromArgb(0x666666);
        if (serviceController?.Status == System.ServiceProcess.ServiceControllerStatus.Running)
        {
            serviceController.Stop();
        }
        RefreshServiceStatus();
    }






}

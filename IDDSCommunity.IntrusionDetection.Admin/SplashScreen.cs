using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SplashScreen : Form
{

    readonly Timer t = new();
    private readonly StartupOperation startupOperation = new();

    /// <summary>
    /// Gets whether both bootstrap work and the administration interface completed initialization.
    /// </summary>
    internal bool StartupSucceeded => startupOperation.Succeeded && IddsAdmin.Instance.IsInitialized;

    /// <summary>
    /// 初始化 <see cref="SplashScreen"/> 類別的新執行個體。
    /// </summary>

    public SplashScreen()
    {
        InitializeComponent();
        BrandingIcons.ApplyTo(pictureBox1);
        Icon = BrandingIcons.CreateIcon();
        smartLabelVersion.Text = string.Format(Strings.Get("Version {0}"), Application.ProductVersion);
        smartLabelStatus.Text = Strings.Get("Loading components...");
        BackColor = Color.White;
        Load += new EventHandler(SplashScreen_Load);
    }

    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void SplashScreen_Load(object? sender, EventArgs e)
    {
        t.Interval = 100;
        t.Tick += new EventHandler(t_Tick);
        t.Start();

    }

    /// <summary>
    /// Starts up components.
    /// </summary>

    public void StartupComponents()
    {
        smartLabelStatus.Text = Strings.Get("Configuring database...");
        Database.Instance.Configure(Application.StartupPath);
        smartLabelStatus.Text = Strings.Get("Checking database...");

        smartLabelStatus.Text = Strings.Get("Setting environment variables...");
        IddsConfig.Instance.ApplicationPath = Application.StartupPath;
        IddsConfig.Instance.PluginsDirectory = Application.StartupPath + "\\Plugins\\";
        smartLabelStatus.Text = Strings.Get("Loading configuration data...");
        IddsConfig.Instance.Load();
        LanguageManager.Instance.Initialize(IddsConfig.Instance.Language);
        smartLabelStatus.Text = Strings.Get("Loading agents...");
        SecurityAgents.Instance.RegisterSecurityAgents();

        smartLabelStatus.Text = Strings.Get("Loading application...");

        IddsAdmin.Instance.PanelSecurityLog.Visible = true; // used to preload element
        IddsAdmin.Instance.InitAdmin();
        IddsAdmin.Instance.Visible = false;
    }

    /// <summary>
    /// 處理 tick 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void t_Tick(object? sender, EventArgs e)
    {
        if (!startupOperation.TryRun(StartupComponents, out Exception? failure))
            return;
        t.Stop();
        if (StartupSucceeded)
        {
            Close();
            return;
        }
        string detail = failure?.Message ?? failure?.GetType().Name ?? Strings.Get("The administration interface did not complete initialization.");
        DialogResult result = MessageBox.Show(
            Strings.Format("Application startup failed due to insufficient permissions. Restart as Administrator?", detail),
            Strings.Get("Administrator Privileges Required"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new(Application.ExecutablePath)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User cancelled the UAC prompt
            }
        }
        Close();
    }

    /// <summary>
    /// Releases the splash timer when the form closes on either success or failure.
    /// </summary>
    /// <param name="e">The form-closed event data.</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        t.Stop();
        t.Dispose();
        base.OnFormClosed(e);
    }

}

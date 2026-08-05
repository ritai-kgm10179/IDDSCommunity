using System;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class SplashScreen : Form
{

    readonly Timer t = new();
    private readonly StartupOperation startupOperation = new();

    /// <summary>
    /// Gets whether both bootstrap work and the administration interface completed initialization.
    /// </summary>
    internal bool StartupSucceeded => startupOperation.Succeeded && IddsAdmin.Instance.IsInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashScreen"/> class.
    /// </summary>

    public SplashScreen()
    {
        InitializeComponent();
        smartLabelVersion.Text = string.Format(Strings.Get("Version {0}"), Application.ProductVersion);
        smartLabelStatus.Text = Strings.Get("Loading components...");
        BackColor = Color.White;
        Load += new EventHandler(SplashScreen_Load);
    }

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
        smartLabelEdition.Text = Strings.Get("Unlimited edition");

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
    /// Handles the tick event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
        string detail = failure?.GetType().Name ?? Strings.Get("The administration interface did not complete initialization.");
        MessageBox.Show(
            Strings.Format("Application startup failed: {0}", detail),
            Strings.Get("Application startup failed"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
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

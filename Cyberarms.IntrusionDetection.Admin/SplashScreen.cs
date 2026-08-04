using System;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class SplashScreen : Form
{

    readonly Timer t = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashScreen"/> class.
    /// </summary>

    public SplashScreen()
    {
        InitializeComponent();
        smartLabelVersion.Text = "Version " + Application.ProductVersion;
        smartLabelStatus.Text = "Loading components...";
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
        t.Start();
        t.Tick += new EventHandler(t_Tick);

    }

    /// <summary>
    /// Starts up components.
    /// </summary>

    public void StartupComponents()
    {
        smartLabelEdition.Text = "Unlimited edition";

        smartLabelStatus.Text = "Configuring database...";
        Database.Instance.Configure(Application.StartupPath);
        smartLabelStatus.Text = "Checking database...";

        smartLabelStatus.Text = "Setting environment variables...";
        IddsConfig.Instance.ApplicationPath = Application.StartupPath;
        IddsConfig.Instance.PluginsDirectory = Application.StartupPath + "\\Plugins\\";
        smartLabelStatus.Text = "Loading configuration data...";
        IddsConfig.Instance.Load();
        smartLabelStatus.Text = "Loading agents...";
        SecurityAgents.Instance.RegisterSecurityAgents();

        smartLabelStatus.Text = "Loading application...";

        IddsAdmin.Instance.PanelSecurityLog.Visible = true; // used to preload element
        IddsAdmin.Instance.InitAdmin();
        IddsAdmin.Instance.Visible = false;
    }

    public bool IsUpdating { get; set; }


    /// <summary>
    /// Handles the tick event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void t_Tick(object? sender, EventArgs e)
    {
        if (!IsUpdating)
        {
            IsUpdating = true;
            StartupComponents();
        }
        if (IddsAdmin.Instance.IsInitialized) Close();
    }

}

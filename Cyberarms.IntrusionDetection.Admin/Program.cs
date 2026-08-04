using System;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        LanguageManager.Instance.Initialize("auto");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        //Application.Run(new Form1());
        Application.Run(new SplashScreen());
        IddsAdmin.Instance.Visible = true;
        Application.Run(IddsAdmin.Instance);
    }
}

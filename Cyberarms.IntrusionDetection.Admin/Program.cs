using System;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
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
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        InitializeDisplayLanguage();
        //Application.Run(new Form1());
        Application.Run(new SplashScreen());
        IddsAdmin.Instance.Visible = true;
        Application.Run(IddsAdmin.Instance);
    }

    /// <summary>
    /// Loads the persisted language before constructing any localized WinForms controls.
    /// </summary>
    private static void InitializeDisplayLanguage()
    {
        LanguageManager.Instance.Initialize("auto");
        try
        {
            Database.Instance.Configure(Application.StartupPath);
            IddsConfig.Instance.Load();
            LanguageManager.Instance.Initialize(IddsConfig.Instance.Language);
        }
        catch (Exception)
        {
            // StartupComponents reports configuration failures; retain the safe system-language fallback here.
        }
    }
}

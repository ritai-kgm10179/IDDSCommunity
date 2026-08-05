using System;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        if (ElevatedServiceCommand.TryExecute(args, out int exitCode))
        {
            Environment.ExitCode = exitCode;
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        InitializeDisplayLanguage();
        //Application.Run(new Form1());
        using SplashScreen splashScreen = new();
        Application.Run(splashScreen);
        if (!splashScreen.StartupSucceeded)
            return;
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

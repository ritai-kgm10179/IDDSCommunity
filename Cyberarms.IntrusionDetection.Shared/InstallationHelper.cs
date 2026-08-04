#if NETFRAMEWORK
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace Cyberarms.IntrusionDetection.Shared;

public class InstallState : Dictionary<string, string> { }
[RunInstaller(true)]
public partial class InstallationHelper : Installer
{
    public override void Install(IDictionary stateSaver)
    {
        base.Install(stateSaver);
    }
    public InstallationHelper()
    {
        InitializeComponent();

        this.AfterInstall += new InstallEventHandler(InstallationHelper_AfterInstall);
        this.BeforeUninstall += new InstallEventHandler(InstallationHelper_BeforeUninstall);
        this.AfterUninstall += new InstallEventHandler(InstallationHelper_AfterUninstall);
        this.BeforeInstall += new InstallEventHandler(InstallationHelper_BeforeInstall);
    }

    void InstallationHelper_BeforeInstall(object sender, InstallEventArgs e)
    {
        if (EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE))
        {
            EventLog.DeleteEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
        }
    }

    void InstallationHelper_AfterUninstall(object sender, InstallEventArgs e)
    {
        try
        {
            if (System.Diagnostics.EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE)) System.Diagnostics.EventLog.DeleteEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
            if (System.Diagnostics.EventLog.Exists(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME)) System.Diagnostics.EventLog.Delete(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME);
        }
        catch
        {
        }
        try
        {
            ProcessStartInfo sInfo = new("https://cyberarms.net/intrusion-detection/sorry-to-see-you-leave.aspx");
            System.Diagnostics.Process.Start(sInfo);
        }
        catch { }

    }

    void InstallationHelper_BeforeUninstall(object sender, InstallEventArgs e)
    {

    }

    void InstallationHelper_AfterInstall(object sender, InstallEventArgs e)
    {
        try
        {
            ProcessStartInfo sInfo = new("https://cyberarms.net/intrusion-detection/whats-next.aspx");
            System.Diagnostics.Process.Start(sInfo);
        }
        catch { }
    }
}
#endif

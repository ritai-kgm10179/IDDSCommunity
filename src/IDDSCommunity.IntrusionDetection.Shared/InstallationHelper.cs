#if NETFRAMEWORK
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace IDDSCommunity.IntrusionDetection.Shared;

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
        if (EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE))
        {
            EventLog.DeleteEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE);
        }
    }

    void InstallationHelper_AfterUninstall(object sender, InstallEventArgs e)
    {
        try
        {
            if (System.Diagnostics.EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE)) System.Diagnostics.EventLog.DeleteEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE);
            if (System.Diagnostics.EventLog.Exists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME)) System.Diagnostics.EventLog.Delete(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME);
        }
        catch
        {
        }
    }

    void InstallationHelper_BeforeUninstall(object sender, InstallEventArgs e)
    {

    }

    void InstallationHelper_AfterInstall(object sender, InstallEventArgs e)
    {
    }
}
#endif

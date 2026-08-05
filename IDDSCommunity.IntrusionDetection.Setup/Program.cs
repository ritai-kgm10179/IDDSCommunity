using System;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class Program
{
    /// <summary>Starts the elevated setup user interface.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
    }
}

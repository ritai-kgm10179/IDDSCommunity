using System;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class Program
{
    /// <summary>
    /// 啟動提升權限之安裝管理使用者介面。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
    }
}

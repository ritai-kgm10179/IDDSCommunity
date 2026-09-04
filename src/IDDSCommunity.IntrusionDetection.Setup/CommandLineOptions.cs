using System;

namespace IDDSCommunity.IntrusionDetection.Setup;

/// <summary>
/// 代表傳入安裝程式之命令列引數選項。
/// </summary>
internal sealed class CommandLineOptions
{
    /// <summary>
    /// 取得一個值，指出是否要求執行安裝或升級作業。
    /// </summary>
    public bool IsInstall { get; init; }

    /// <summary>
    /// 取得一個值，指出是否要求執行解除安裝作業。
    /// </summary>
    public bool IsUninstall { get; init; }

    /// <summary>
    /// 取得一個值，指出是否以無人值守靜默模式執行。
    /// </summary>
    public bool IsQuiet { get; init; }

    /// <summary>
    /// 取得一個值，指出是否略過建立桌面捷徑。
    /// </summary>
    public bool NoDesktop { get; init; }

    /// <summary>
    /// 取得一個值，指出是否略過建立開始功能表捷徑。
    /// </summary>
    public bool NoStartMenu { get; init; }

    /// <summary>
    /// 解析命令列引數字串陣列為結構化選項物件。
    /// </summary>
    /// <param name="args">命令列引數字串陣列。</param>
    /// <returns>傳回已解析之 <see cref="CommandLineOptions"/> 執行個體。</returns>
    public static CommandLineOptions Parse(string[] args)
    {
        bool isInstall = false;
        bool isUninstall = false;
        bool isQuiet = false;
        bool noDesktop = false;
        bool noStartMenu = false;

        foreach (string rawArg in args)
        {
            string arg = rawArg.Trim();
            if (string.Equals(arg, "/install", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-install", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--install", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "/i", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-i", StringComparison.OrdinalIgnoreCase))
            {
                isInstall = true;
            }
            else if (string.Equals(arg, "/uninstall", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-uninstall", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "/u", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-u", StringComparison.OrdinalIgnoreCase))
            {
                isUninstall = true;
            }
            else if (string.Equals(arg, "/quiet", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-quiet", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "--quiet", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "/silent", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-silent", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "/q", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-q", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "/s", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-s", StringComparison.OrdinalIgnoreCase))
            {
                isQuiet = true;
            }
            else if (string.Equals(arg, "/nodesktop", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-nodesktop", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "--nodesktop", StringComparison.OrdinalIgnoreCase))
            {
                noDesktop = true;
            }
            else if (string.Equals(arg, "/nostartmenu", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "-nostartmenu", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "--nostartmenu", StringComparison.OrdinalIgnoreCase))
            {
                noStartMenu = true;
            }
        }

        return new CommandLineOptions
        {
            IsInstall = isInstall,
            IsUninstall = isUninstall,
            IsQuiet = isQuiet,
            NoDesktop = noDesktop,
            NoStartMenu = noStartMenu
        };
    }
}

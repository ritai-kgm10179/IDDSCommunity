using System;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;
using Strings = IDDSCommunity.IntrusionDetection.Shared.Localization.Strings;

namespace IDDSCommunity.DatabaseDiagnostics;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--help" or "-h" or "/?")
        {
            PrintUsage();
            return 0;
        }

        string databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "IDDS Community",
            "iddscommunity.dbf");
        string outputPath = Path.Combine(
            Environment.CurrentDirectory,
            $"idds-database-diagnostics-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json");

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--database" when index + 1 < args.Length:
                    databasePath = args[++index];
                    break;
                case "--output" when index + 1 < args.Length:
                    outputPath = args[++index];
                    break;
                default:
                    Console.Error.WriteLine(Strings.Format("Unknown or incomplete argument: {0}", args[index]));
                    PrintUsage();
                    return 2;
            }
        }

        try
        {
            DatabaseDiagnosticExporter.Export(databasePath, outputPath);
            Console.WriteLine(Strings.Get("The diagnostic summary was exported:"));
            Console.WriteLine(Path.GetFullPath(outputPath));
            Console.WriteLine(Strings.Get("The summary does not contain IP addresses, accounts, event details, or the database key."));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(Strings.Format("Diagnostic export failed: {0}", exception.Message));
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(Strings.Get("IDDSCommunity.DatabaseDiagnostics [--database <dbf path>] [--output <json path>]"));
        Console.WriteLine(Strings.Get("By default, the tool reads %ProgramData%\\IDDS Community\\iddscommunity.dbf and writes to the current directory."));
    }
}

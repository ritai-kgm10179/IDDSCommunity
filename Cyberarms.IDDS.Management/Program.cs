using System;
using System.IO;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IDDS.Management;

class Program
{

    /// <summary>
    /// Runs the application entry point.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        path = path[..(path.LastIndexOf('\\') + 1)];

        // Console.WriteLine("Directory: {0}", path);
        if (args.Length == 0 || args[0].ToLower() == "help")
        {
            ShowUsage();
            return;
        }
        try
        {
            Database.Instance.Configure(path);
            IddsConfig.Instance.ApplicationPath = path;
            switch (args[0].ToLower())
            {
                case "islicensed":
                    Console.WriteLine(@"Cyberarms IDDS does not require licensing anymore. Please visit https://cyberarms.net for support options!");
                    break;
                case "manualreport":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please enter output filename as parameter.");
                        return;
                    }
                    string report = ReportGenerator.Instance.GetReport(string.Format("Manual report, {0:MM/dd/yyyy}", DateTime.Now),
                        string.Format("Manually created report for system {0}.{1}", System.Net.Dns.GetHostName(), System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName), ""
                        , DateTime.Now.AddMonths(-1), DateTime.Now);
                    try
                    {
                        StreamWriter sw = File.CreateText(args[1]);
                        sw.Write(report);
                        sw.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
                case "exportcsv":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please enter output filename as parameter.");
                        return;
                    }
                    int lastSquenceNumber = 0;
                    if (args.Length > 2)
                    {
                        if (!int.TryParse(args[2], out lastSquenceNumber))
                        {
                            Console.WriteLine("Please enter last sequence number as parameter.");
                        }
                    }
                    try
                    {
                        StreamWriter swexport = File.CreateText(args[1]);
                        System.Data.IDataReader rdr = IntrusionLog.ReadDifferential(lastSquenceNumber);
                        swexport.WriteLine("Sequence Number;Date and Time;Security Agent;IP Address;Status");
                        while (rdr.Read())
                        {
                            swexport.WriteLine("{0};{1};{2};{3};{4}", rdr[0].ToString(), rdr[1].ToString(), SecurityAgents.Instance.GetDisplayName(rdr[2]?.ToString() ?? string.Empty),
                                rdr[3].ToString(), IntrusionLog.GetStatusName(int.Parse(rdr[4]?.ToString() ?? "0")));
                        }
                        swexport.Flush();
                        swexport.Close();
                        rdr.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
                default:
                    ShowUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

    /// <summary>
    /// Executes the show usage operation.
    /// </summary>

    static void ShowUsage()
    {
        Console.WriteLine("Cyberarms.IDDS.Management - iddsadmin.exe");
        Console.WriteLine("Usage:");
        Console.WriteLine("iddsadmin command [parameters]");
        Console.WriteLine("-----------------------------------------------------------------------");
        Console.WriteLine("Commands");
        Console.WriteLine("    manualreport <file>      create report for the last month to <file> (html)");
        Console.WriteLine("    exportcsv <file>         export all data to file <file>");
        Console.WriteLine("    exportcsv <file> <seq>   export data since sequence number <seq> to <file>");
        Console.WriteLine("");
        Console.WriteLine("(c) 2013-2016 Cyberarms is a trademark of isicore");
    }

}

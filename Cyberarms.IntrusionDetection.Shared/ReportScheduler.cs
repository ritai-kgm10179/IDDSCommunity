using System;
using System.Timers;

namespace Cyberarms.IntrusionDetection.Shared;

public class ReportScheduler
{
    Timer reporter;

    public event EventHandler RunDailyReport;
    public event EventHandler RunWeeklyReport;
    public event EventHandler RunMonthlyReport;

    private ReportScheduler()
    {
    }

    private static ReportScheduler _instance;
    public static ReportScheduler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ReportScheduler();
                _instance.Init();
            }
            return _instance;
        }

        set => _instance = value;
    }

    private void Init()
    {
        reporter = new Timer(600000);
        reporter.Elapsed += new ElapsedEventHandler(reporter_Elapsed);
    }

    public void StartReporting() => reporter.Start();

    void reporter_Elapsed(object sender, ElapsedEventArgs e)
    {
        NotificationSettings.Reload();
        if (NotificationSettings.Instance.SummaryReportDaily) CheckDailyReport();
        if (NotificationSettings.Instance.SummaryReportWeekly) CheckWeeklyReport();
        if (NotificationSettings.Instance.SummaryReportMonthly) CheckMonthlyReport();
    }

    public void CheckDailyReport()
    {
        NotificationSettings.Reload();
        DateTime d = DateTime.Now.AddDays(-1);
        string dailyReportTime = string.Format("{0}-{1}-{2}", d.Year, d.Month, d.Day);
        if (!string.Equals(dailyReportTime, NotificationSettings.LastDailyReport))
        {
            // run daily report
            NotificationSettings.LastDailyReport = dailyReportTime;
            NotificationSettings.Save();
            RunDailyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CheckWeeklyReport()
    {
        NotificationSettings.Reload();
        DateTime d = DateTime.Now.AddDays(-1);
        string weeklyReportTime = GetWeekOfYearString(d);
        if (GetWeekOfYear(d) != GetWeekOfYear(DateTime.Now) && !string.Equals(weeklyReportTime, NotificationSettings.LastWeeklyReport))
        {
            // run weekly report
            NotificationSettings.LastWeeklyReport = weeklyReportTime;
            NotificationSettings.Save();
            RunWeeklyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CheckMonthlyReport()
    {
        NotificationSettings.Reload();
        DateTime d = DateTime.Now.AddDays(-1);
        string monthlyReportTime = string.Format("{0}-{1}", d.Year, d.Month);
        if (d.Month != DateTime.Now.Month && !string.Equals(monthlyReportTime, NotificationSettings.LastMonthlyReport))
        {
            // run monthly report
            NotificationSettings.LastMonthlyReport = monthlyReportTime;
            NotificationSettings.Save();
            RunMonthlyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetWeekOfYearString(DateTime d)
    {
        int weekOfYear = GetWeekOfYear(d);
        int year = d.Year;
        if (weekOfYear > 50 && d.Month < 2) year--;
        return string.Format("{0}-{1}", year, weekOfYear);
    }

    public static int GetWeekOfYear(DateTime d)
    {
        System.Globalization.GregorianCalendar cal = new(System.Globalization.GregorianCalendarTypes.Localized);
        int weekOfYear = cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekOfYear;
    }

}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Shared;

public class ReportScheduler
{
    private readonly TimeProvider timeProvider;
    private CancellationTokenSource? cancellation;

    public event EventHandler? RunDailyReport;
    public event EventHandler? RunWeeklyReport;
    public event EventHandler? RunMonthlyReport;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportScheduler"/> class.
    /// </summary>

    private ReportScheduler() : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a scheduler with an explicit time source for deterministic tests.
    /// </summary>
    /// <param name="timeProvider">The source of current time and timer ticks.</param>
    internal ReportScheduler(TimeProvider timeProvider) => this.timeProvider = timeProvider;

    private static ReportScheduler? _instance;
    public static ReportScheduler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ReportScheduler();
            }
            return _instance;
        }

        set => _instance = value;
    }

    /// <summary>
    /// Starts reporting.
    /// </summary>

    public void StartReporting()
    {
        if (cancellation is not null)
            return;
        cancellation = new CancellationTokenSource();
        _ = RunAsync(cancellation.Token);
    }

    /// <summary>
    /// Stops the reporting loop.
    /// </summary>

    public void StopReporting()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }

    /// <summary>
    /// Runs the reporting checks at a fixed interval using the configured time provider.
    /// </summary>
    /// <param name="cancellationToken">Stops the reporting loop.</param>
    /// <returns>A task representing the reporting loop.</returns>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10), timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                NotificationSettings.Reload();
                if (NotificationSettings.Instance.SummaryReportDaily) CheckDailyReport();
                if (NotificationSettings.Instance.SummaryReportWeekly) CheckWeeklyReport();
                if (NotificationSettings.Instance.SummaryReportMonthly) CheckMonthlyReport();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    /// <summary>
    /// Executes the check daily report operation.
    /// </summary>

    public void CheckDailyReport()
    {
        NotificationSettings.Reload();
        DateTime d = timeProvider.GetLocalNow().DateTime.AddDays(-1);
        string dailyReportTime = string.Format("{0}-{1}-{2}", d.Year, d.Month, d.Day);
        if (!string.Equals(dailyReportTime, NotificationSettings.LastDailyReport))
        {
            // run daily report
            NotificationSettings.LastDailyReport = dailyReportTime;
            NotificationSettings.Save();
            RunDailyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Executes the check weekly report operation.
    /// </summary>

    public void CheckWeeklyReport()
    {
        NotificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string weeklyReportTime = GetWeekOfYearString(d);
        if (GetWeekOfYear(d) != GetWeekOfYear(now) && !string.Equals(weeklyReportTime, NotificationSettings.LastWeeklyReport))
        {
            // run weekly report
            NotificationSettings.LastWeeklyReport = weeklyReportTime;
            NotificationSettings.Save();
            RunWeeklyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Executes the check monthly report operation.
    /// </summary>

    public void CheckMonthlyReport()
    {
        NotificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string monthlyReportTime = string.Format("{0}-{1}", d.Year, d.Month);
        if (d.Month != now.Month && !string.Equals(monthlyReportTime, NotificationSettings.LastMonthlyReport))
        {
            // run monthly report
            NotificationSettings.LastMonthlyReport = monthlyReportTime;
            NotificationSettings.Save();
            RunMonthlyReport?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets week of year string.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <returns>The get week of year string result.</returns>

    public string GetWeekOfYearString(DateTime d)
    {
        int weekOfYear = GetWeekOfYear(d);
        int year = d.Year;
        if (weekOfYear > 50 && d.Month < 2) year--;
        return string.Format("{0}-{1}", year, weekOfYear);
    }

    /// <summary>
    /// Gets week of year.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <returns>The get week of year result.</returns>

    public static int GetWeekOfYear(DateTime d)
    {
        System.Globalization.GregorianCalendar cal = new(System.Globalization.GregorianCalendarTypes.Localized);
        int weekOfYear = cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekOfYear;
    }

}

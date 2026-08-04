using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Shared;

public class ReportScheduler
{
    private readonly TimeProvider timeProvider;
    private CancellationTokenSource? cancellation;

    /// <summary>
    /// Gets or sets how often enabled reports are checked.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(10);

    public event Func<CancellationToken, Task>? RunDailyReportAsync;
    public event Func<CancellationToken, Task>? RunWeeklyReportAsync;
    public event Func<CancellationToken, Task>? RunMonthlyReportAsync;

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
        using PeriodicTimer timer = new(CheckInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                NotificationSettings.Reload();
                try
                {
                    if (NotificationSettings.Instance.SummaryReportDaily) await CheckDailyReportAsync(cancellationToken).ConfigureAwait(false);
                    if (NotificationSettings.Instance.SummaryReportWeekly) await CheckWeeklyReportAsync(cancellationToken).ConfigureAwait(false);
                    if (NotificationSettings.Instance.SummaryReportMonthly) await CheckMonthlyReportAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // The report handler logs the operational error. Leave the checkpoint unchanged so the next tick retries it.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    /// <summary>
    /// Executes the check daily report operation.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report generation.</param>
    /// <returns>A task that completes after the report succeeds or no report is due.</returns>

    public async Task CheckDailyReportAsync(CancellationToken cancellationToken = default)
    {
        NotificationSettings.Reload();
        DateTime d = timeProvider.GetLocalNow().DateTime.AddDays(-1);
        string dailyReportTime = string.Format("{0}-{1}-{2}", d.Year, d.Month, d.Day);
        if (!string.Equals(dailyReportTime, NotificationSettings.LastDailyReport))
        {
            // run daily report
            await InvokeAsync(RunDailyReportAsync, cancellationToken).ConfigureAwait(false);
            NotificationSettings.LastDailyReport = dailyReportTime;
            NotificationSettings.Save();
        }
    }

    /// <summary>
    /// Executes the check weekly report operation.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report generation.</param>
    /// <returns>A task that completes after the report succeeds or no report is due.</returns>

    public async Task CheckWeeklyReportAsync(CancellationToken cancellationToken = default)
    {
        NotificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string weeklyReportTime = GetWeekOfYearString(d);
        if (GetWeekOfYear(d) != GetWeekOfYear(now) && !string.Equals(weeklyReportTime, NotificationSettings.LastWeeklyReport))
        {
            // run weekly report
            await InvokeAsync(RunWeeklyReportAsync, cancellationToken).ConfigureAwait(false);
            NotificationSettings.LastWeeklyReport = weeklyReportTime;
            NotificationSettings.Save();
        }
    }

    /// <summary>
    /// Executes the check monthly report operation.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report generation.</param>
    /// <returns>A task that completes after the report succeeds or no report is due.</returns>

    public async Task CheckMonthlyReportAsync(CancellationToken cancellationToken = default)
    {
        NotificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string monthlyReportTime = string.Format("{0}-{1}", d.Year, d.Month);
        if (d.Month != now.Month && !string.Equals(monthlyReportTime, NotificationSettings.LastMonthlyReport))
        {
            // run monthly report
            await InvokeAsync(RunMonthlyReportAsync, cancellationToken).ConfigureAwait(false);
            NotificationSettings.LastMonthlyReport = monthlyReportTime;
            NotificationSettings.Save();
        }
    }

    /// <summary>
    /// Invokes asynchronous report handlers sequentially and propagates failures to preserve retry semantics.
    /// </summary>
    /// <param name="handlers">The handlers to invoke.</param>
    /// <param name="cancellationToken">Signals cancellation of report generation.</param>
    /// <returns>A task that completes after every handler succeeds.</returns>
    private static async Task InvokeAsync(Func<CancellationToken, Task>? handlers, CancellationToken cancellationToken)
    {
        if (handlers is null)
            return;

        foreach (Delegate subscriber in handlers.GetInvocationList())
            await ((Func<CancellationToken, Task>)subscriber)(cancellationToken).ConfigureAwait(false);
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

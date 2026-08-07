using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class ReportScheduler
{
    private readonly TimeProvider timeProvider;
    private readonly NotificationSettings notificationSettings;
    private CancellationTokenSource? cancellation;
    /// <summary>
    /// 取得或設定已啟用報表的檢查間隔時間。
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>
    /// 取得排程器迴圈是否已啟動。
    /// </summary>
    public bool IsRunning => cancellation is not null;

    public event Func<CancellationToken, Task>? RunDailyReportAsync;
    public event Func<CancellationToken, Task>? RunWeeklyReportAsync;
    public event Func<CancellationToken, Task>? RunMonthlyReportAsync;
    /// <summary>
    /// 初始化 <see cref="ReportScheduler"/> class的新執行個體。
    /// </summary>

    private ReportScheduler() : this(TimeProvider.System, NotificationSettings.Instance)
    {
    }
    /// <summary>
    /// 初始化具備明確時間來源的排程器以供確定性測試。
    /// </summary>
    /// <param name="timeProvider">目前時間與定時器 Tick 的來源。</param>
    internal ReportScheduler(TimeProvider timeProvider) : this(timeProvider, NotificationSettings.Instance)
    {
    }
    /// <summary>
    /// 初始化具備明確時間與通知相依性的排程器。
    /// </summary>
    /// <param name="timeProvider">目前時間與定時器 Tick 的來源。</param>
    /// <param name="notificationSettings">持久化的報表設定與檢查點。</param>
    public ReportScheduler(TimeProvider timeProvider, NotificationSettings notificationSettings)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(notificationSettings);
        this.timeProvider = timeProvider;
        this.notificationSettings = notificationSettings;
    }

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
    }
    /// <summary>
    /// 啟動報表排程作業。
    /// </summary>

    public void StartReporting()
    {
        if (cancellation is not null)
            return;
        cancellation = new CancellationTokenSource();
        _ = RunAsync(cancellation.Token);
    }
    /// <summary>
    /// 停止報表排程迴圈。
    /// </summary>

    public void StopReporting()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
    /// <summary>
    /// 使用設定的時間提供者以固定間隔執行報表檢查。
    /// </summary>
    /// <param name="cancellationToken">停止報表排程迴圈。</param>
    /// <returns>傳回代表報表排程迴圈的 Task。</returns>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(CheckInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    notificationSettings.Reload();
                    if (notificationSettings.SummaryReportDaily) await CheckDailyReportAsync(cancellationToken).ConfigureAwait(false);
                    if (notificationSettings.SummaryReportWeekly) await CheckWeeklyReportAsync(cancellationToken).ConfigureAwait(false);
                    if (notificationSettings.SummaryReportMonthly) await CheckMonthlyReportAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Trace.TraceError("Scheduled report check failed: {0}", exception);
                    // Leave the checkpoint unchanged so the next tick retries it.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    /// <summary>
    /// 執行check daily report作業。
    /// </summary>
    /// <param name="cancellationToken">發送取消報表生成作業的訊號。</param>
    /// <returns>傳回待報表成功完成或無待處理報表後結束的 Task。</returns>

    public async Task CheckDailyReportAsync(CancellationToken cancellationToken = default)
    {
        notificationSettings.Reload();
        DateTime d = timeProvider.GetLocalNow().DateTime.AddDays(-1);
        string dailyReportTime = string.Format("{0}-{1}-{2}", d.Year, d.Month, d.Day);
        if (!string.Equals(dailyReportTime, notificationSettings.LastDailyReport))
        {
            // run daily report
            await DeliverAsync(
                RunDailyReportAsync,
                state => notificationSettings.DailyReportState = state,
                () => notificationSettings.LastDailyReport = dailyReportTime,
                cancellationToken).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// 執行check weekly report作業。
    /// </summary>
    /// <param name="cancellationToken">發送取消報表生成作業的訊號。</param>
    /// <returns>傳回待報表成功完成或無待處理報表後結束的 Task。</returns>

    public async Task CheckWeeklyReportAsync(CancellationToken cancellationToken = default)
    {
        notificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string weeklyReportTime = GetWeekOfYearString(d);
        if (GetWeekOfYear(d) != GetWeekOfYear(now) && !string.Equals(weeklyReportTime, notificationSettings.LastWeeklyReport))
        {
            // run weekly report
            await DeliverAsync(
                RunWeeklyReportAsync,
                state => notificationSettings.WeeklyReportState = state,
                () => notificationSettings.LastWeeklyReport = weeklyReportTime,
                cancellationToken).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// 執行check monthly report作業。
    /// </summary>
    /// <param name="cancellationToken">發送取消報表生成作業的訊號。</param>
    /// <returns>傳回待報表成功完成或無待處理報表後結束的 Task。</returns>

    public async Task CheckMonthlyReportAsync(CancellationToken cancellationToken = default)
    {
        notificationSettings.Reload();
        DateTime now = timeProvider.GetLocalNow().DateTime;
        DateTime d = now.AddDays(-1);
        string monthlyReportTime = string.Format("{0}-{1}", d.Year, d.Month);
        if (d.Month != now.Month && !string.Equals(monthlyReportTime, notificationSettings.LastMonthlyReport))
        {
            // run monthly report
            await DeliverAsync(
                RunMonthlyReportAsync,
                state => notificationSettings.MonthlyReportState = state,
                () => notificationSettings.LastMonthlyReport = monthlyReportTime,
                cancellationToken).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// 持久化報表傳送狀態轉換，並僅於傳送成功後推進檢查點。
    /// </summary>
    /// <param name="handlers">報表傳送處理常式。</param>
    /// <param name="setState">更新報表傳送狀態。</param>
    /// <param name="advanceCheckpoint">推進成功傳送之報表檢查點。</param>
    /// <param name="cancellationToken">發送取消傳送作業的訊號。</param>
    /// <returns>傳回待持久化狀態更新完成後結束的 Task。</returns>
    private async Task DeliverAsync(
        Func<CancellationToken, Task>? handlers,
        Action<ReportDeliveryState> setState,
        Action advanceCheckpoint,
        CancellationToken cancellationToken)
    {
        setState(ReportDeliveryState.Pending);
        notificationSettings.Save();
        setState(ReportDeliveryState.Sending);
        notificationSettings.Save();
        try
        {
            await InvokeAsync(handlers, cancellationToken).ConfigureAwait(false);
            advanceCheckpoint();
            setState(ReportDeliveryState.Succeeded);
            notificationSettings.Save();
        }
        catch
        {
            setState(ReportDeliveryState.Failed);
            notificationSettings.Save();
            throw;
        }
    }
    /// <summary>
    /// 依序呼叫非同步報表處理常式並傳播失敗資訊以保留重試語意。
    /// </summary>
    /// <param name="handlers">要呼叫的處理常式。</param>
    /// <param name="cancellationToken">發送取消報表生成作業的訊號。</param>
    /// <returns>傳回待所有處理常式成功後結束的 Task。</returns>
    private static async Task InvokeAsync(Func<CancellationToken, Task>? handlers, CancellationToken cancellationToken)
    {
        if (handlers is null)
            return;

        foreach (Delegate subscriber in handlers.GetInvocationList())
            await ((Func<CancellationToken, Task>)subscriber)(cancellationToken).ConfigureAwait(false);
    }
    /// <summary>
    /// 取得年份週次字串。
    /// </summary>
    /// <param name="d">d參數。</param>
    /// <returns>傳回get week of year string結果。</returns>

    public string GetWeekOfYearString(DateTime d)
    {
        int weekOfYear = GetWeekOfYear(d);
        int year = d.Year;
        if (weekOfYear > 50 && d.Month < 2) year--;
        return string.Format("{0}-{1}", year, weekOfYear);
    }
    /// <summary>
    /// 取得年份週次。
    /// </summary>
    /// <param name="d">d參數。</param>
    /// <returns>傳回get week of year結果。</returns>

    public static int GetWeekOfYear(DateTime d)
    {
        System.Globalization.GregorianCalendar cal = new(System.Globalization.GregorianCalendarTypes.Localized);
        int weekOfYear = cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekOfYear;
    }

}

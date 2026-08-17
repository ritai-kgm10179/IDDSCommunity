using System;
using System.Diagnostics.Eventing.Reader;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 以 <see cref="EventLogWatcher"/> 訂閱單一 Windows 事件記錄頻道，並針對每筆事件記錄嘗試解析出
/// 驗證失敗事件。
/// </summary>
public sealed class WindowsEventLogFailureSource : IAuthenticationEventSource
{
    private readonly string channel;
    private readonly string query;
    private readonly Func<EventRecord, AuthenticationFailureEvent?> parser;
    private EventLogWatcher? watcher;

    /// <summary>
    /// 初始化 <see cref="WindowsEventLogFailureSource"/> 類別的新執行個體。
    /// </summary>
    /// <param name="channel">欲訂閱之事件記錄頻道名稱。</param>
    /// <param name="query">用於篩選事件之 XPath 查詢字串。</param>
    /// <param name="parser">將事件記錄解析為驗證失敗事件的委派。</param>
    public WindowsEventLogFailureSource(string channel, string query, Func<EventRecord, AuthenticationFailureEvent?> parser)
    {
        this.channel = channel;
        this.query = query;
        this.parser = parser;
    }

    /// <summary>
    /// 當解析出驗證失敗事件時引發。
    /// </summary>
    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    /// <summary>
    /// 當訂閱或解析事件發生例外狀況時引發。
    /// </summary>
    public event Action<Exception>? Error;

    /// <summary>
    /// 建立並啟用事件記錄監看器。
    /// </summary>
    public void Start()
    {
        if (watcher is not null) return;
        watcher = new EventLogWatcher(new EventLogQuery(channel, PathType.LogName, query), null, false);
        watcher.EventRecordWritten += OnEvent;
        watcher.Enabled = true;
    }

    /// <summary>
    /// 暫停事件記錄監看器。
    /// </summary>
    public void Pause() { if (watcher is not null) watcher.Enabled = false; }
    /// <summary>
    /// 從暫停狀態恢復事件記錄監看器。
    /// </summary>
    public void Resume() { if (watcher is not null) watcher.Enabled = true; }
    /// <summary>
    /// 停止並釋放事件記錄監看器。
    /// </summary>
    public void Stop()
    {
        if (watcher is null) return;
        watcher.Enabled = false;
        watcher.EventRecordWritten -= OnEvent;
        watcher.Dispose();
        watcher = null;
    }
    /// <summary>
    /// 停止事件記錄監看器並釋放相關資源。
    /// </summary>
    public void Dispose() => Stop();

    private void OnEvent(object? sender, EventRecordWrittenEventArgs args)
    {
        if (args.EventException is not null) { Error?.Invoke(args.EventException); return; }
        using EventRecord? record = args.EventRecord;
        if (record is null) return;
        try
        {
            AuthenticationFailureEvent? failure = parser(record);
            if (failure is not null) EventReceived?.Invoke(this, failure);
        }
        catch (Exception exception) { Error?.Invoke(exception); }
    }
}

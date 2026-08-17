using System;
using System.Collections.Generic;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 將多個 <see cref="IAuthenticationEventSource"/> 組合為單一事件來源，統一轉發事件與錯誤，
/// 使 Agent 得以同時監看多種來源（例如 Windows 事件記錄與文字記錄檔）。
/// </summary>
public sealed class CompositeAuthenticationEventSource : IAuthenticationEventSource
{
    private readonly IReadOnlyList<IAuthenticationEventSource> sources;

    /// <summary>
    /// 初始化 <see cref="CompositeAuthenticationEventSource"/> 類別的新執行個體。
    /// </summary>
    /// <param name="sources">欲組合之事件來源集合。</param>
    public CompositeAuthenticationEventSource(params IAuthenticationEventSource[] sources)
    {
        this.sources = sources;
        foreach (IAuthenticationEventSource source in sources)
        {
            source.EventReceived += Forward;
            source.Error += ForwardError;
        }
    }

    /// <summary>
    /// 當任一子來源解析出驗證失敗事件時引發。
    /// </summary>
    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    /// <summary>
    /// 當任一子來源發生例外狀況時引發。
    /// </summary>
    public event Action<Exception>? Error;
    /// <summary>
    /// 啟動所有子事件來源。
    /// </summary>
    public void Start() { foreach (IAuthenticationEventSource source in sources) source.Start(); }
    /// <summary>
    /// 暫停所有子事件來源。
    /// </summary>
    public void Pause() { foreach (IAuthenticationEventSource source in sources) source.Pause(); }
    /// <summary>
    /// 從暫停狀態恢復所有子事件來源。
    /// </summary>
    public void Resume() { foreach (IAuthenticationEventSource source in sources) source.Resume(); }
    /// <summary>
    /// 停止所有子事件來源。
    /// </summary>
    public void Stop() { foreach (IAuthenticationEventSource source in sources) source.Stop(); }
    /// <summary>
    /// 取消訂閱並釋放所有子事件來源。
    /// </summary>
    public void Dispose()
    {
        foreach (IAuthenticationEventSource source in sources)
        {
            source.EventReceived -= Forward;
            source.Error -= ForwardError;
            source.Dispose();
        }
    }

    private void Forward(object? sender, AuthenticationFailureEvent failure) => EventReceived?.Invoke(this, failure);
    private void ForwardError(Exception exception) => Error?.Invoke(exception);
}

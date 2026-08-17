using System;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 定義可供 <see cref="AuthenticationAgentBase{TConfiguration}"/> 訂閱之驗證失敗事件來源的共同介面。
/// </summary>
public interface IAuthenticationEventSource : IDisposable
{
    /// <summary>
    /// 當解析出驗證失敗事件時引發。
    /// </summary>
    event EventHandler<AuthenticationFailureEvent>? EventReceived;
    /// <summary>
    /// 當事件來源發生例外狀況時引發。
    /// </summary>
    event Action<Exception>? Error;
    /// <summary>
    /// 啟動事件來源並開始接收事件。
    /// </summary>
    void Start();
    /// <summary>
    /// 暫停事件接收，但保留已建立之訂閱狀態。
    /// </summary>
    void Pause();
    /// <summary>
    /// 從暫停狀態恢復事件接收。
    /// </summary>
    void Resume();
    /// <summary>
    /// 停止事件來源並釋放其佔用之非受控資源。
    /// </summary>
    void Stop();
}

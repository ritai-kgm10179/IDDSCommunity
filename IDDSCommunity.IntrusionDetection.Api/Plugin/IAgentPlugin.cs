namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 傳送入侵攻擊事件至入侵偵測服務之委派事件處理常式。
/// </summary>
/// <param name="sender">發送事件之 Agent 物件。</param>
/// <param name="data">入侵事件通知詳細資料。</param>
public delegate void AttackDetectedHandler(object sender, INotificationEventArgs data);
/// <summary>
/// 入侵偵測 Agent 擴充元件必須實作之介面。
/// </summary>
public interface IAgentPlugin
{
    /// <summary>
    /// 當偵測到攻擊時發生的事件。
    /// </summary>
    /// <seealso cref="AttackDetectedHandler"/>
    event AttackDetectedHandler AttackDetected;
    /// <summary>
    /// 啟動 Agent 命令（於服務啟動時呼叫）。
    /// </summary>
    void Start();
    /// <summary>
    /// 停止 Agent 命令（於服務停止時呼叫）。
    /// </summary>
    void Stop();
    /// <summary>
    /// 暫停 Agent 命令（於服務暫停時呼叫）。
    /// </summary>
    void Pause();
    /// <summary>
    /// 繼續執行 Agent 命令（自暫停狀態復原時呼叫）。
    /// </summary>
    void Continue();
    /// <summary>
    /// 傳回 Agent 是否支援暫停。
    /// </summary>
    /// <returns>若支援暫停傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    bool CanPause();
    /// <summary>
    /// 傳回 Agent 目前是否可進行繼續執行。
    /// </summary>
    /// <returns>若可繼續執行傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    bool CanContinue();
    /// <summary>
    /// 取得或設定 Agent 是否處於暫停狀態。
    /// </summary>
    bool IsPaused { get; set; }
    /// <summary>
    /// 取得 Agent 是否處於執行狀態。
    /// </summary>
    bool IsRunning { get; }
    /// <summary>
    /// 取得或設定 Agent 設定物件。
    /// </summary>
    IAgentConfiguration Configuration { get; set; }
}

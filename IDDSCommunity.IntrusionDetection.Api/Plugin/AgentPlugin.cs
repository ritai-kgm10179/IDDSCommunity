using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// Agent 擴充元件之基底類別。
/// </summary>
public class AgentPlugin : IAgentPlugin
{
    private readonly object lifecycleSync = new();
    private bool isPaused;
    private bool isRunning;

    /// <summary>
    /// 當偵測到入侵攻擊時發生的事件。
    /// </summary>
    public event AttackDetectedHandler? AttackDetected;

    /// <summary>
    /// 初始化 <see cref="AgentPlugin"/> 類別的新執行個體。
    /// </summary>
    public AgentPlugin() => IsPaused = false;

    /// <summary>
    /// 引發偵測到攻擊事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="data">包含攻擊資訊的事件資料。</param>
    protected void OnAttackDetected(object sender, INotificationEventArgs data)
    {
        foreach (AttackDetectedHandler handler in AttackDetected?.GetInvocationList() ?? [])
        {
            try
            {
                handler(this, data);
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Api.Plugin.AgentPlugin", ex.ToString());
                }
                catch (Exception logException)
                {
                    System.Diagnostics.Trace.TraceError("Agent event handler failed: {0}; event-log fallback failed: {1}", ex, logException);
                }
            }
        }
    }

    /// <summary>
    /// 啟動 Agent 服務。
    /// </summary>
    public void Start()
    {
        lock (lifecycleSync)
        {
            if (isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent is already running. Operation cancelled!"));
            OnStartAgent();
            isRunning = true;
            isPaused = false;
        }
    }

    /// <summary>
    /// 停止 Agent 服務。
    /// </summary>
    public void Stop()
    {
        lock (lifecycleSync)
        {
            if (!isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent is not running."));
            OnStopAgent();
            isRunning = false;
            isPaused = false;
        }
    }

    /// <summary>
    /// 暫停 Agent 服務。
    /// </summary>
    public void Pause()
    {
        lock (lifecycleSync)
        {
            if (isPaused || !isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent cannot be paused in this state"));
            OnPauseAgent();
            isPaused = true;
        }
    }

    /// <summary>
    /// 繼續執行暫停的 Agent 服務。
    /// </summary>
    public void Continue()
    {
        lock (lifecycleSync)
        {
            if (!isPaused || !isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent must be in paused state"));
            OnContinueAgent();
            isPaused = false;
        }
    }

    /// <summary>
    /// 判斷目前 Agent 是否可進行暫停。
    /// </summary>
    /// <returns>若可暫停傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanPause()
    {
        lock (lifecycleSync)
            return !isPaused && isRunning;
    }

    /// <summary>
    /// 判斷目前 Agent 是否可繼續執行。
    /// </summary>
    /// <returns>若可繼續執行傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanContinue()
    {
        lock (lifecycleSync)
            return isPaused && isRunning;
    }

    /// <summary>
    /// 取得或設定 Agent 是否處於暫停狀態。
    /// </summary>
    public bool IsPaused
    {
        get
        {
            lock (lifecycleSync)
                return isPaused;
        }
        set
        {
            lock (lifecycleSync)
                isPaused = value;
        }
    }

    /// <summary>
    /// 取得 Agent 是否正在執行中。
    /// </summary>
    public virtual bool IsRunning
    {
        get
        {
            lock (lifecycleSync)
                return isRunning;
        }
    }

    private IAgentConfiguration? _configuration;

    /// <summary>
    /// 取得或設定 Agent 的設定物件。
    /// </summary>
    public IAgentConfiguration Configuration
    {
        get => _configuration ??= new AgentConfigurationBase();
        set => _configuration = value;
    }

    /// <summary>
    /// 處理啟動 Agent 的通知。
    /// </summary>
    protected virtual void OnStartAgent() { }

    /// <summary>
    /// 處理暫停 Agent 的通知。
    /// </summary>
    protected virtual void OnPauseAgent() { }

    /// <summary>
    /// 處理停止 Agent 的通知。
    /// </summary>
    protected virtual void OnStopAgent() { }

    /// <summary>
    /// 處理繼續執行 Agent 的通知。
    /// </summary>
    protected virtual void OnContinueAgent() { }
}

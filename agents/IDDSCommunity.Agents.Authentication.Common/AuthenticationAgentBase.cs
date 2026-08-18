using System;
using System.Diagnostics;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 提供以共用驗證失敗偵測框架（滑動時間窗、事件去重、門檻值判斷）為基礎之 Agent 抽象基底類別；
/// 衍生類別只需提供事件來源與解析邏輯，即可自動獲得一致的偵測行為。
/// </summary>
/// <typeparam name="TConfiguration">此 Agent 使用之設定型別，須衍生自 <see cref="AuthenticationAgentConfiguration"/>。</typeparam>
public abstract class AuthenticationAgentBase<TConfiguration> : AgentPlugin, IExtendedInformation
    where TConfiguration : AuthenticationAgentConfiguration, new()
{
    private readonly IAuthenticationEventSource source;
    private AuthenticationThresholdDetector? detector;

    /// <summary>
    /// 初始化 <see cref="AuthenticationAgentBase{TConfiguration}"/> 類別的新執行個體。
    /// </summary>
    /// <param name="source">用於接收驗證失敗事件的來源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 為 <see langword="null"/>。</exception>
    protected AuthenticationAgentBase(IAuthenticationEventSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        Configuration = new AgentConfigurationBase
        {
            AgentName = GetType().Name,
            ConfigurationSettingsTypeName = typeof(TConfiguration).FullName ?? string.Empty,
            AgentSettings = new TConfiguration()
        };
    }

    /// <summary>
    /// 驗證目前設定、建立門檻偵測器，並啟動事件來源。
    /// </summary>
    /// <exception cref="InvalidOperationException">設定物件型別不符或設定驗證失敗。</exception>
    protected override void OnStartAgent()
    {
        TConfiguration configuration = GetConfiguration();
        configuration.Validate();
        detector = new AuthenticationThresholdDetector(configuration);
        source.EventReceived += OnEventReceived;
        source.Error += OnSourceError;
        try { source.Start(); }
        catch
        {
            source.EventReceived -= OnEventReceived;
            source.Error -= OnSourceError;
            detector = null;
            throw;
        }
    }

    /// <summary>
    /// 暫停底層事件來源的事件接收。
    /// </summary>
    protected override void OnPauseAgent() => source.Pause();
    /// <summary>
    /// 從暫停狀態恢復底層事件來源的事件接收。
    /// </summary>
    protected override void OnContinueAgent() => source.Resume();

    /// <summary>
    /// 停止並釋放底層事件來源，同時清除門檻偵測器狀態。
    /// </summary>
    protected override void OnStopAgent()
    {
        source.EventReceived -= OnEventReceived;
        source.Error -= OnSourceError;
        source.Stop();
        source.Dispose();
        detector = null;
    }

    /// <summary>
    /// 取得一個值，指出此 Agent 是否使用內部本機門檻計數器。
    /// 預設為 <see langword="true"/>；若由 Phase 0 跨來源引擎統一計算門檻，衍生類別可覆寫為 <see langword="false"/> 以避免重複計數。
    /// </summary>
    protected virtual bool UseLocalThresholdDetector => true;

    /// <summary>
    /// 處理事件來源回報的驗證失敗事件；僅於門檻偵測器判定已達攻擊門檻（或已委由中央引擎處理）時引發攻擊偵測通知。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="failure">已解析之驗證失敗事件。</param>
    private void OnEventReceived(object? sender, AuthenticationFailureEvent failure)
    {
        if (UseLocalThresholdDetector && detector?.Analyze(failure) != true)
            return;

        string message = UseLocalThresholdDetector
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, IntrusionDetection.Api.Localization.Strings.Get("{0}: authentication failure threshold exceeded."), failure.Category)
            : $"{failure.Category}: {failure.Reason}";

        OnAttackDetected(this, new AuthenticationNotificationEventArgs
        {
            CreateDate = failure.OccurredAt.LocalDateTime,
            EventId = failure.EventId,
            IpAddress = failure.SourceAddress.ToString(),
            EventMessage = message,
            AccountName = failure.AccountName,
            IsCredentialFailure = failure.IsCredentialFailure,
            ProviderOrChannel = failure.ProviderOrChannel,
            ComputerName = failure.ComputerName,
            SourceEventRecordId = failure.SourceEventRecordId,
            ActivityId = failure.ActivityId,
            ConfidenceScore = failure.ConfidenceScore,
            TargetResource = failure.TargetResource,
            ErrorCode = failure.ErrorCode
        });
    }

    /// <summary>
    /// 記錄事件來源回報的例外狀況，不中止事件訂閱。
    /// </summary>
    /// <param name="exception">事件來源回報之例外狀況。</param>
    private void OnSourceError(Exception exception) => Trace.TraceError("{0}: {1}", DisplayName, exception.Message);
    /// <summary>
    /// 取得已強型別化之 Agent 設定物件。
    /// </summary>
    /// <returns>目前的 <typeparamref name="TConfiguration"/> 設定執行個體。</returns>
    /// <exception cref="InvalidOperationException">設定物件尚未配置或型別不符。</exception>
    protected TConfiguration GetConfiguration() => Configuration.AgentSettings as TConfiguration ?? throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Agent configuration is unavailable."));

    /// <summary>
    /// 取得或設定 Agent 於管理介面中顯示的名稱。
    /// </summary>
    public abstract string DisplayName { get; set; }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public abstract Guid Id { get; }
    /// <summary>
    /// 取得或設定 Agent 的預設圖示；回傳 <see langword="null"/> 時由主程式套用統一主題圖示。
    /// </summary>
    public Image? Icon { get => null; set { } }
    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的圖示；回傳 <see langword="null"/> 時由主程式套用統一主題圖示。
    /// </summary>
    public Image? SelectedIcon { get => null; set { } }
    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的圖示；回傳 <see langword="null"/> 時由主程式套用統一主題圖示。
    /// </summary>
    public Image? UnselectedIcon { get => null; set { } }
}

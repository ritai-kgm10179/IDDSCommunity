namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;

/// <summary>
/// 代表來源事件流之處理水位點模型，記錄特定代理與記錄通道最後一次成功處理的記錄識別與時間。
/// </summary>
public sealed class ObservationWatermark
{
    /// <summary>
    /// 初始化 <see cref="ObservationWatermark"/> 類別的新執行個體。
    /// </summary>
    public ObservationWatermark()
    {
        SourceAgentName = string.Empty;
        ProviderOrChannel = string.Empty;
        LastTimestampUtc = DateTimeOffset.MinValue;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 初始化 <see cref="ObservationWatermark"/> 類別的新執行個體並設定初始值。
    /// </summary>
    /// <param name="sourceAgentName">來源擴充元件代理名稱。</param>
    /// <param name="providerOrChannel">事件提供者或通道名稱。</param>
    /// <param name="lastEventRecordId">最後處理之事件記錄編號。</param>
    /// <param name="lastTimestampUtc">最後處理事件之時間戳記。</param>
    public ObservationWatermark(
        string sourceAgentName,
        string providerOrChannel,
        long? lastEventRecordId,
        DateTimeOffset lastTimestampUtc)
    {
        SourceAgentName = sourceAgentName ?? string.Empty;
        ProviderOrChannel = providerOrChannel ?? string.Empty;
        LastEventRecordId = lastEventRecordId;
        LastTimestampUtc = lastTimestampUtc;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 取得或設定來源擴充元件代理名稱。
    /// </summary>
    public string SourceAgentName { get; set; }

    /// <summary>
    /// 取得或設定事件提供者或通道名稱。
    /// </summary>
    public string ProviderOrChannel { get; set; }

    /// <summary>
    /// 取得或設定最後成功處理之 Windows 事件記錄編號 (EventRecordID)。
    /// </summary>
    public long? LastEventRecordId { get; set; }

    /// <summary>
    /// 取得或設定最後成功處理之事件 UTC 時間戳記。
    /// </summary>
    public DateTimeOffset LastTimestampUtc { get; set; }

    /// <summary>
    /// 取得或設定水位點記錄之最後更新時間。
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// 取得水位點字典識別鍵值（格式為「來源代理名稱|通道名稱」）。
    /// </summary>
    public string Key => $"{SourceAgentName}|{ProviderOrChannel}";
}

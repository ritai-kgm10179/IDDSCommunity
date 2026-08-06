namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 網路監聽器擴充元件介面。
/// </summary>
public interface INetworkListener
{
    /// <summary>
    /// 取得或設定已處理之封包總數。
    /// </summary>
    long TotalPackets { get; set; }
}

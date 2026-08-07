using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 包含攻擊者資訊之事件通知參數基底類別。
/// </summary>
public class NotificationEventArgs : INotificationEventArgs
{
    /// <summary>
    /// 取得或設定攻擊者的 IP 位址（支援 IPv4 點分十進位格式或 IPv6 格式）。
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    /// <summary>
    /// 取得或設定通知發生時間。
    /// </summary>
    public DateTime CreateDate { get; set; }
    /// <summary>
    /// 取得或設定事件識別碼。
    /// </summary>
    public int EventId { get; set; }
    /// <summary>
    /// 取得或設定傳遞至事件監聽器的選擇性訊息。
    /// </summary>
    public string EventMessage { get; set; } = string.Empty;
}

using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 包含攻擊者資訊之事件通知參數介面。
/// </summary>
public interface INotificationEventArgs
{
    /// <summary>
    /// 取得或設定攻擊者的 IP 位址（支援 IPv4 點分十進位格式或 IPv6 格式）。
    /// </summary>
    string IpAddress { get; set; }

    /// <summary>
    /// 取得或設定通知發生時間。
    /// </summary>
    DateTime CreateDate { get; set; }

    /// <summary>
    /// 取得或設定事件識別碼。
    /// </summary>
    int EventId { get; set; }

    /// <summary>
    /// 取得或設定傳遞至事件監聽器的選擇性訊息。
    /// </summary>
    string EventMessage { get; set; }
}

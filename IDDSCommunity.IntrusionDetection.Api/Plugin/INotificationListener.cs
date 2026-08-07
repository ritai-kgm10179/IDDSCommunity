namespace IDDSCommunity.IntrusionDetection.Api.Plugin;
/// <summary>
/// 通知接收器擴充元件介面。
/// </summary>
public interface INotificationListener
{
    /// <summary>
    /// 入侵偵測系統呼叫此方法以轉發通知事件資料。
    /// </summary>
    /// <param name="args">事件通知參數。</param>
    void NotificationReceiver(INotificationEventArgs args);
}

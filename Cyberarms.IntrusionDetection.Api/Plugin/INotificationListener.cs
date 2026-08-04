namespace Cyberarms.IntrusionDetection.Api.Plugin;

/// <summary>
/// NotificationReceiver
/// </summary>
public interface INotificationListener
{
    /// <summary>
    ///Intrusion Detectioncalls the NotificationReceiver to forward notification event data
    /// </summary>
    /// <param name="args"></param>
    void NotificationReceiver(INotificationEventArgs args);
}

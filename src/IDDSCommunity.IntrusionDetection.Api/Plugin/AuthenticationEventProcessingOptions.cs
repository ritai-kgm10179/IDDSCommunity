namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 提供主控服務與驗證擴充元件之間的事件處理模式契約。
/// </summary>
public static class AuthenticationEventProcessingOptions
{
    private static volatile bool enableRawEvents;

    /// <summary>
    /// 取得或設定是否將尚未達本機門檻的原始驗證失敗事件傳送至主控服務。
    /// </summary>
    public static bool EnableRawEvents
    {
        get => enableRawEvents;
        set => enableRawEvents = value;
    }
}

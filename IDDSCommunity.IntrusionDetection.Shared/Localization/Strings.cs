using System.Resources;

namespace IDDSCommunity.IntrusionDetection.Shared.Localization;

public static class Strings
{
    private static ResourceManager? _resourceManager;

    public static ResourceManager ResourceManager
    {
        get
        {
            _resourceManager ??= new ResourceManager("IDDSCommunity.IntrusionDetection.Shared.Localization.Strings", typeof(Strings).Assembly);
            return _resourceManager;
        }
    }
    /// <summary>
    /// 取得 localized user-facing string from the shared string resources.
    /// </summary>
    /// <param name="key">不變資源金鑰。</param>
    /// <returns>傳回在地化數值；當資源不存在時傳回 <paramref name="key"/>。</returns>
    public static string Get(string key) => LanguageManager.Instance.GetString(key, key);
    /// <summary>
    /// 使用選取的應用程式文化特性格式化在地化的使用者介面字串。
    /// </summary>
    /// <param name="key">不變資源金鑰與後備格式字串。</param>
    /// <param name="arguments">插入在地化格式的引數。</param>
    /// <returns>傳回符合文化特性的格式化在地化數值。</returns>
    public static string Format(string key, params object?[] arguments) =>
        string.Format(LanguageManager.Instance.CurrentCulture, Get(key), arguments);

    public static string AppTitle => LanguageManager.Instance.GetString(nameof(AppTitle), "IDDSCommunity Intrusion Detection");
    public static string StatusRunning => LanguageManager.Instance.GetString(nameof(StatusRunning), "Running");
    public static string StatusStopped => LanguageManager.Instance.GetString(nameof(StatusStopped), "Stopped");
    public static string StatusPaused => LanguageManager.Instance.GetString(nameof(StatusPaused), "Paused");
    public static string AttackDetected => LanguageManager.Instance.GetString(nameof(AttackDetected), "Attack Detected");
    public static string SoftLockApplied => LanguageManager.Instance.GetString(nameof(SoftLockApplied), "Soft Lock Applied");
    public static string HardLockApplied => LanguageManager.Instance.GetString(nameof(HardLockApplied), "Hard Lock Applied");
    public static string IPUnlocked => LanguageManager.Instance.GetString(nameof(IPUnlocked), "IP Unlocked");
    public static string ConfigurationSaved => LanguageManager.Instance.GetString(nameof(ConfigurationSaved), "Configuration Saved Successfully");
}

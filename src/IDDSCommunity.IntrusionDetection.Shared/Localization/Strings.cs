using System.Resources;

namespace IDDSCommunity.IntrusionDetection.Shared.Localization;

/// <summary>
/// 提供強型別本地化資源字串存取之核心類別。
/// </summary>
public static class Strings
{
    private static ResourceManager? _resourceManager;

        /// <summary>
    /// 取得或設定 ResourceManager。
    /// </summary>
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

        /// <summary>
    /// 取得或設定 AppTitle。
    /// </summary>
public static string AppTitle => LanguageManager.Instance.GetString(nameof(AppTitle), "IDDSCommunity Intrusion Detection");
        /// <summary>
    /// 取得或設定 StatusRunning。
    /// </summary>
public static string StatusRunning => LanguageManager.Instance.GetString(nameof(StatusRunning), "Running");
        /// <summary>
    /// 取得或設定 StatusStopped。
    /// </summary>
public static string StatusStopped => LanguageManager.Instance.GetString(nameof(StatusStopped), "Stopped");
        /// <summary>
    /// 取得或設定 StatusPaused。
    /// </summary>
public static string StatusPaused => LanguageManager.Instance.GetString(nameof(StatusPaused), "Paused");
        /// <summary>
    /// 取得或設定 AttackDetected。
    /// </summary>
public static string AttackDetected => LanguageManager.Instance.GetString(nameof(AttackDetected), "Attack Detected");
        /// <summary>
    /// 取得或設定 SoftLockApplied。
    /// </summary>
public static string SoftLockApplied => LanguageManager.Instance.GetString(nameof(SoftLockApplied), "Soft Lock Applied");
        /// <summary>
    /// 取得或設定 HardLockApplied。
    /// </summary>
public static string HardLockApplied => LanguageManager.Instance.GetString(nameof(HardLockApplied), "Hard Lock Applied");
        /// <summary>
    /// 取得或設定 IPUnlocked。
    /// </summary>
public static string IPUnlocked => LanguageManager.Instance.GetString(nameof(IPUnlocked), "IP Unlocked");
        /// <summary>
    /// 取得或設定 ConfigurationSaved。
    /// </summary>
public static string ConfigurationSaved => LanguageManager.Instance.GetString(nameof(ConfigurationSaved), "Configuration Saved Successfully");
}

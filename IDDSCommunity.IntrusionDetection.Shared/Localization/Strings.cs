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
    /// Gets a localized user-facing string from the shared string resources.
    /// </summary>
    /// <param name="key">The invariant resource key.</param>
    /// <returns>The localized value, or <paramref name="key"/> when the resource is missing.</returns>
    public static string Get(string key) => LanguageManager.Instance.GetString(key, key);

    /// <summary>
    /// Formats a localized user-facing string using the selected application culture.
    /// </summary>
    /// <param name="key">The invariant resource key and fallback format.</param>
    /// <param name="arguments">The values inserted into the localized format.</param>
    /// <returns>The localized and culture-aware formatted value.</returns>
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

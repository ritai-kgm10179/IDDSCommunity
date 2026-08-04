using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace Cyberarms.IntrusionDetection.Shared.Localization;

public enum SupportedLanguage
{
    Auto,
    English,
    TraditionalChinese
}

public class LanguageManager
{
    public const string DEFAULT_CULTURE = "en-US";
    public const string TRADITIONAL_CHINESE_CULTURE = "zh-TW";

    private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();
    private CultureInfo _currentCulture = new(DEFAULT_CULTURE);
    private readonly Lock _lock = new();

    private static LanguageManager? _instance;
    public static LanguageManager Instance
    {
        get
        {
            _instance ??= new LanguageManager();
            return _instance;
        }
    }

    private LanguageManager()
    {
        RegisterResourceManager("Strings", Strings.ResourceManager);
        Initialize("auto");
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public void RegisterResourceManager(string name, ResourceManager resourceManager) => _resourceManagers[name] = resourceManager;

    public void Initialize(string? userLanguageSetting)
    {
        lock (_lock)
        {
            CultureInfo targetCulture;

            if (string.IsNullOrEmpty(userLanguageSetting) || string.Equals(userLanguageSetting, "auto", StringComparison.OrdinalIgnoreCase))
            {
                targetCulture = DetectSystemCultureWithFallback();
            }
            else if (string.Equals(userLanguageSetting, "zh-TW", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(userLanguageSetting, "zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(userLanguageSetting, "zh", StringComparison.OrdinalIgnoreCase))
            {
                targetCulture = new CultureInfo(TRADITIONAL_CHINESE_CULTURE);
            }
            else
            {
                targetCulture = new CultureInfo(DEFAULT_CULTURE);
            }

            _currentCulture = targetCulture;
            CultureInfo.DefaultThreadCurrentCulture = targetCulture;
            CultureInfo.DefaultThreadCurrentUICulture = targetCulture;
            Thread.CurrentThread.CurrentCulture = targetCulture;
            Thread.CurrentThread.CurrentUICulture = targetCulture;
        }
    }

    public static CultureInfo DetectSystemCultureWithFallback()
    {
        CultureInfo currentUI = CultureInfo.CurrentUICulture;
        string name = currentUI.Name;

        if (name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
        {
            return new CultureInfo(TRADITIONAL_CHINESE_CULTURE);
        }

        // Fallback for all other unsupported cultures (e.g. ja-JP, fr-FR, de-DE) to English (en-US)
        return new CultureInfo(DEFAULT_CULTURE);
    }

    public string GetString(string key, string? defaultValue = null, string resourceCategory = "Strings")
    {
        if (_resourceManagers.TryGetValue(resourceCategory, out ResourceManager? resourceManager))
        {
            try
            {
                string? val = resourceManager.GetString(key, _currentCulture);
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch { }
        }
        return defaultValue ?? key;
    }
}

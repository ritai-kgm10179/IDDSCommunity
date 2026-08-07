using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Shared.Localization;

public enum SupportedLanguage
{
    Auto,
    English,
    TraditionalChinese
}

public sealed class LanguageManager
{
    public const string DEFAULT_CULTURE = "en-US";
    public const string TRADITIONAL_CHINESE_CULTURE = "zh-TW";

    private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();
    private CultureInfo _currentCulture = new(DEFAULT_CULTURE);
    private readonly Lock _lock = new();

    private static readonly Lazy<LanguageManager> LazyInstance = new(() => new LanguageManager(), true);
    public static LanguageManager Instance => LazyInstance.Value;
    /// <summary>
    /// 初始化 <see cref="LanguageManager"/> class的新執行個體。
    /// </summary>

    private LanguageManager()
    {
        RegisterResourceManager("Strings", Strings.ResourceManager);
        Initialize("auto");
    }

    public CultureInfo CurrentCulture => _currentCulture;
    /// <summary>
    /// 執行register resource manager作業。
    /// </summary>
    /// <param name="name">name參數。</param>
    /// <param name="resourceManager">resource manager參數。</param>

    public void RegisterResourceManager(string name, ResourceManager resourceManager) => _resourceManagers[name] = resourceManager;
    /// <summary>
    /// 執行initialize作業。
    /// </summary>
    /// <param name="userLanguageSetting">user language setting參數。</param>

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
            else if (string.Equals(userLanguageSetting, "en", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(userLanguageSetting, DEFAULT_CULTURE, StringComparison.OrdinalIgnoreCase))
            {
                targetCulture = new CultureInfo(DEFAULT_CULTURE);
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
    /// <summary>
    /// 執行detect system culture with fallback作業。
    /// </summary>
    /// <returns>傳回detect system culture with fallback結果。</returns>

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
    /// <summary>
    /// 取得字串。
    /// </summary>
    /// <param name="key">key參數。</param>
    /// <param name="defaultValue">default value參數。</param>
    /// <param name="resourceCategory">resource category參數。</param>
    /// <returns>傳回get string結果。</returns>

    public string GetString(string key, string? defaultValue = null, string resourceCategory = "Strings")
    {
        if (_resourceManagers.TryGetValue(resourceCategory, out ResourceManager? resourceManager))
        {
            try
            {
                string? val = resourceManager.GetString(key, _currentCulture);
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch (MissingManifestResourceException)
            {
                return defaultValue ?? key;
            }
        }
        return defaultValue ?? key;
    }
}

using System.Globalization;
using System.Resources;

namespace Cyberarms.IntrusionDetection.Api.Localization;

/// <summary>
/// Provides localized strings for the dependency-free public API and agent implementations.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager = new("Cyberarms.IntrusionDetection.Api.Localization.Strings", typeof(Strings).Assembly);

    /// <summary>
    /// Gets a localized string for the current UI culture.
    /// </summary>
    /// <param name="key">The invariant resource key.</param>
    /// <returns>The localized value, or <paramref name="key"/> when missing.</returns>
    public static string Get(string key) => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}

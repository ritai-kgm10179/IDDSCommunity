using System.Globalization;
using System.Resources;

namespace IDDSCommunity.Agents.WindowsDns;

internal static class DnsStrings
{
    private static readonly ResourceManager ResourceManager = new("IDDSCommunity.Agents.WindowsDns.Resources", typeof(DnsStrings).Assembly);

    /// <summary>
    /// Gets one localized DNS Agent string with invariant fallback.
    /// </summary>
    /// <param name="key">The invariant resource key and fallback value.</param>
    /// <returns>The localized value.</returns>
    internal static string Get(string key) => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>
    /// Formats one localized DNS Agent string using the current UI culture.
    /// </summary>
    /// <param name="key">The invariant resource key and fallback format.</param>
    /// <param name="arguments">The values inserted into the localized format.</param>
    /// <returns>The localized formatted value.</returns>
    internal static string Format(string key, params object?[] arguments) => string.Format(CultureInfo.CurrentUICulture, Get(key), arguments);
}

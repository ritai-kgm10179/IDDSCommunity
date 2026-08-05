using System.Globalization;
using System.Resources;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class SetupText
{
    private static readonly ResourceManager Resources = new("IDDSCommunity.IntrusionDetection.Setup.SetupStrings", typeof(SetupText).Assembly);

    /// <summary>Gets a localized setup string.</summary>
    /// <param name="name">The resource name.</param>
    /// <returns>The localized value.</returns>
    internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    /// <summary>Formats a localized setup string.</summary>
    /// <param name="name">The resource name.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The formatted localized value.</returns>
    internal static string Format(string name, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
}

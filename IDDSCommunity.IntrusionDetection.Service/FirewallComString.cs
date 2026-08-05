using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// Converts managed strings to temporary COM BSTR values for source-generated Windows Firewall interfaces.
/// </summary>
internal static class FirewallComString
{
    /// <summary>
    /// Gets the managed representation of a COM BSTR value.
    /// </summary>
    /// <param name="value">The COM string.</param>
    /// <returns>The managed string, or an empty string when the pointer is null.</returns>
    internal static string Get(BSTR value) => value.ToString() ?? string.Empty;

    /// <summary>
    /// Invokes a COM property setter with a temporary BSTR that is always released.
    /// </summary>
    /// <param name="value">The managed string.</param>
    /// <param name="setter">The COM setter to invoke before releasing the temporary allocation.</param>
    internal static void Set(string value, Action<BSTR> setter)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(setter);
        IntPtr pointer = Marshal.StringToBSTR(value);
        try
        {
            setter((BSTR)pointer);
        }
        finally
        {
            Marshal.FreeBSTR(pointer);
        }
    }
}

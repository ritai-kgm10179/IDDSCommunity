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
    /// <returns>受控字串；若指標為 null 則傳回空字串。</returns>
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

    /// <summary>
    /// 使用暫時性 BSTR 叫用 COM 函式並傳回結果，確保 BSTR 記憶體一定會被釋放。
    /// </summary>
    /// <typeparam name="T">傳回值型別。</typeparam>
    /// <param name="value">受控字串值。</param>
    /// <param name="func">接收 BSTR 並傳回結果之委派。</param>
    /// <returns>傳回委派執行結果。</returns>
    internal static T Call<T>(string value, Func<BSTR, T> func)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(func);
        IntPtr pointer = Marshal.StringToBSTR(value);
        try
        {
            return func((BSTR)pointer);
        }
        finally
        {
            Marshal.FreeBSTR(pointer);
        }
    }
}

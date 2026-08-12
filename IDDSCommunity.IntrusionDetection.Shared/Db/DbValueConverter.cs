using System;

namespace IDDSCommunity.IntrusionDetection.Shared.Db;

public class DbValueConverter
{
    /// <summary>
    /// 執行to bool作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool ToBool(object? value)
    {
        if (value is null or DBNull) return false;
        if (value is bool b) return b;
        string str = value.ToString() ?? string.Empty;
        if (bool.TryParse(str, out bool result)) return result;
        if (int.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int intVal)) return intVal != 0;
        return false;
    }
    /// <summary>
    /// 執行to string作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回to string結果。</returns>
    public static string ToString(object? value)
    {
        if (value is null or DBNull) return string.Empty;
        return value.ToString() ?? string.Empty;
    }
    /// <summary>
    /// 執行to int作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回to int結果。</returns>
    public static int ToInt(object? value)
    {
        if (value is null or DBNull) return 0;
        int.TryParse(value.ToString(), out int result);
        return result;
    }
    /// <summary>
    /// 執行to int64作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回to int64結果。</returns>
    public static long ToInt64(object? value)
    {
        if (value is null or DBNull) return 0;
        long.TryParse(value.ToString(), out long result);
        return result;
    }
    /// <summary>
    /// 執行to guid作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回to guid結果。</returns>
    public static Guid ToGuid(object? value)
    {
        string textValue = ToString(value);
        if (!Guid.TryParse(textValue, out Guid result))
        {
            throw new ArgumentException(value + " is not a unique id");
        }
        return result;
    }
    /// <summary>
    /// 執行to date time作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>傳回to date time結果。</returns>
    public static DateTime ToDateTime(object? value)
    {
        if (value is null or DBNull) return DateTime.MinValue;
        if (!DateTime.TryParse(ToString(value), out DateTime result))
        {
            throw new ArgumentException(value + " is not a valid date");
        }
        return result;
    }
}

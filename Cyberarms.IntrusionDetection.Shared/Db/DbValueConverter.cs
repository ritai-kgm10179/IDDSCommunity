using System;

namespace Cyberarms.IntrusionDetection.Shared.Db;

public class DbValueConverter
{
    /// <summary>
    /// Executes the to bool operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

    public static bool ToBool(object? value)
    {
        if (value is null or DBNull) return false;
        bool.TryParse(value.ToString(), out bool result);
        return result;
    }

    /// <summary>
    /// Executes the to string operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The to string result.</returns>

    public static string ToString(object? value)
    {
        if (value is null or DBNull) return string.Empty;
        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Executes the to int operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The to int result.</returns>

    public static int ToInt(object? value)
    {
        if (value is null or DBNull) return 0;
        int.TryParse(value.ToString(), out int result);
        return result;
    }

    /// <summary>
    /// Executes the to int64 operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The to int64 result.</returns>

    public static long ToInt64(object? value)
    {
        if (value is null or DBNull) return 0;
        long.TryParse(value.ToString(), out long result);
        return result;
    }

    /// <summary>
    /// Executes the to guid operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The to guid result.</returns>

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
    /// Executes the to date time operation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The to date time result.</returns>

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

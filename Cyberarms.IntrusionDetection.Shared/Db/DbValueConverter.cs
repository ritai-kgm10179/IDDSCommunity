using System;

namespace Cyberarms.IntrusionDetection.Shared.Db;

public class DbValueConverter
{
    public static bool ToBool(object value)
    {
        if (value == DBNull.Value) return false;
        bool.TryParse(value.ToString(), out bool result);
        return result;
    }

    public static string ToString(object value)
    {
        if (value == DBNull.Value) return string.Empty;
        return value.ToString();
    }

    public static int ToInt(object value)
    {
        if (value == DBNull.Value) return 0;
        int.TryParse(value.ToString(), out int result);
        return result;
    }

    public static long ToInt64(object value)
    {
        if (value == DBNull.Value) return 0;
        long.TryParse(value.ToString(), out long result);
        return result;
    }

    public static Guid ToGuid(object value)
    {
        string textValue = ToString(value);
        if (!Guid.TryParse(textValue, out Guid result))
        {
            throw new ArgumentException(value + " is not a unique id");
        }
        return result;
    }

    public static DateTime ToDateTime(object value)
    {
        if (value == DBNull.Value) return DateTime.MinValue;
        if (!DateTime.TryParse(ToString(value), out DateTime result))
        {
            throw new ArgumentException(value + " is not a valid date");
        }
        return result;
    }
}

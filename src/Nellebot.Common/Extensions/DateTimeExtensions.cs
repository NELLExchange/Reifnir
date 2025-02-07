using System;

namespace Nellebot.Common.Extensions;

public static class DateTimeExtensions
{
    public static string ToIsoDateTimeString(this DateTimeOffset dateTimeOffset)
    {
        return $"{dateTimeOffset:yyyy-MM-dd HH:mm}";
    }

    public static string ToIsoDateTimeString(this DateTime dateTime)
    {
        return $"{dateTime:yyyy-MM-dd HH:mm}";
    }
}

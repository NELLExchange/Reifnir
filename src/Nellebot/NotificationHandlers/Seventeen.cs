using System;
using System.Linq;

namespace Nellebot.NotificationHandlers;

public static class Seventeen
{
    public static bool IsDissallowed(string input)
    {
        if (input == "1706" || input == "1707")
        {
            return false;
        }

        var sunrise = new DateTimeOffset(
            year: 2025,
            month: 5,
            day: 17,
            hour: 4,
            minute: 35,
            second: 0,
            TimeSpan.FromHours(2));

        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        if (currentTime >= sunrise) return false;

        return true;
    }
}

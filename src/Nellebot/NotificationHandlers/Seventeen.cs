using System;
using System.Linq;

namespace Nellebot.NotificationHandlers;

public static class Seventeen
{
    public static bool IsMatch(string input)
    {
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

        const string numberString = "1705";

        // Remove all non-digits from input
        string numberOnlyInput = string.Join(string.Empty, input.ToCharArray().Where(char.IsDigit));

        if (numberOnlyInput.Equals(numberString, StringComparison.OrdinalIgnoreCase))
            return true;

        string[] splitBySpace = input.Split(' ');

        var otherHotwords = new[] { "syttendemai", "seventeenohfive", "17o5" };

        int intersectedCount = splitBySpace.Intersect(otherHotwords, StringComparer.OrdinalIgnoreCase).Count();

        return intersectedCount > 0;
    }
}

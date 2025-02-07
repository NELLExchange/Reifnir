namespace Nellebot.Common.Extensions;

public static class StringExtensions
{
    public static string RemoveQuotes(this string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : input.Trim('"');
    }

    public static string GetFirstLine(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        string firstLine = input.Split('\n')[0];

        return firstLine.Length > maxLength ? $"{firstLine[..(maxLength - 3)]}..." : firstLine;
    }
}

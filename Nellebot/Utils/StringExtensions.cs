using System.Text;

namespace Nellebot.Utils;

public static class StringExtensions
{
    // ReSharper disable once InconsistentNaming
    public static StringBuilder AppendLineLF(this StringBuilder sb, string value)
    {
        return sb.Append(value).Append('\n');
    }

    // ReSharper disable once InconsistentNaming
    public static StringBuilder AppendLineLF(this StringBuilder sb)
    {
        return sb.Append('\n');
    }
}

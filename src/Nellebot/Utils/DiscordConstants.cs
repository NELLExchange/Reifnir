using System;

namespace Nellebot.Utils;

public class DiscordConstants
{
    public const int MaxMessageLength = 2000;
    public const int MaxEmbedContentLength = 4096;
    public const int MaxThreadTitleLength = 100;
    public const int DefaultEmbedColor = 2346204; // #23ccdc
    public const int ErrorEmbedColor = 14431557; // #dc3545
    public const int WarningEmbedColor = 16612884; // #fd7e14

    public const char NewLineChar = '\n';
    public const string SlashCommandPrefix = "/";

    public static readonly DateTimeOffset DiscordEpoch = new(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly long DiscordEpochMs = DiscordEpoch.ToUnixTimeMilliseconds();
}

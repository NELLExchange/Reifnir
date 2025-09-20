using System;

namespace Nellebot.Utils;

public static class DiscordConstants
{
    public const int MaxMessageLength = 2000;
    public const int MaxEmbedContentLength = 4096;
    public const int MaxThreadTitleLength = 100;
    public const int MaxAuditReasonLength = 512;
    public const int DefaultEmbedColor = 2346204; // #23ccdc
    public const int ErrorEmbedColor = 14431557; // #dc3545
    public const int WarningEmbedColor = 16612884; // #fd7e14

    public const char NewLineChar = '\n';
    public const string SlashCommandPrefix = "/";

    public static readonly long DiscordEpochMs =
        new DateTimeOffset(year: 2015, month: 1, day: 1, hour: 0, minute: 0, second: 0, TimeSpan.Zero)
            .ToUnixTimeMilliseconds();

    public static readonly TimeSpan MaxDeferredInteractionWait = TimeSpan.FromMinutes(15);
}

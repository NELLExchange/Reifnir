using System.Collections.Generic;

namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordGuild
{
    public required ulong Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyDictionary<ulong, AppDiscordRole> Roles { get; init; }

    public required IReadOnlyDictionary<ulong, AppDiscordChannel> Channels { get; init; }

    public required IReadOnlyDictionary<ulong, AppDiscordEmoji> Emojis { get; init; }
}

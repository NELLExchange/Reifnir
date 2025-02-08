using System;

namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordMessage
{
    public ulong Id { get; set; }

    public AppDiscordUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public AppDiscordChannel Channel { get; set; } = null!;

    public DateTimeOffset CreationTimestamp { get; set; }
}

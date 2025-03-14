namespace Nellebot.Common.AppDiscordModels;

public record AppDiscordEmoji
{
    public required ulong Id { get; set; }

    public required string Name { get; set; }

    public bool IsAnimated { get; set; }
}

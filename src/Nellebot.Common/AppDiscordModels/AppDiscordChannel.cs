namespace Nellebot.Common.AppDiscordModels;

public record AppDiscordChannel
{
    public required ulong Id { get; init; }

    public required string Name { get; init; }
}

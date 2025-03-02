namespace Nellebot.Common.AppDiscordModels;

public record AppDiscordRole
{
    public required ulong Id { get; init; }

    public required string Name { get; init; }

    public bool HasAdminPermission { get; init; }
}

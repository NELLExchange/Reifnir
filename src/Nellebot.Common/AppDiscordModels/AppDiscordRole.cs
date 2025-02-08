namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordRole
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool HasAdminPermission { get; set; }
}

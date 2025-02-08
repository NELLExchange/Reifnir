using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordRoleMapper
{
    public static AppDiscordRole Map(DiscordRole discordRole)
    {
        var appDiscordRole = new AppDiscordRole
        {
            Id = discordRole.Id,
            Name = discordRole.Name,
            HasAdminPermission = discordRole.Permissions.HasPermission(DiscordPermissions.Administrator),
        };

        return appDiscordRole;
    }
}

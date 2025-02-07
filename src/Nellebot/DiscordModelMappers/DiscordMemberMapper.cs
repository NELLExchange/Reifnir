using System.Linq;
using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordMemberMapper
{
    public static AppDiscordMember Map(DiscordMember discordMember)
    {
        AppDiscordUser appDiscordUser = DiscordUserMapper.Map(discordMember);

        var appDiscordMember = new AppDiscordMember
        {
            Id = appDiscordUser.Id,
            Username = appDiscordUser.Username,

            DisplayName = discordMember.DisplayName,
            Roles = discordMember.Roles.Select(DiscordRoleMapper.Map),
        };

        return appDiscordMember;
    }
}

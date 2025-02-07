using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordUserMapper
{
    public static AppDiscordUser Map(DiscordUser discordUser)
    {
        var appDiscordUser = new AppDiscordUser
        {
            Id = discordUser.Id,
            Username = discordUser.Username,
            Discriminator = discordUser.Discriminator,
        };

        return appDiscordUser;
    }
}

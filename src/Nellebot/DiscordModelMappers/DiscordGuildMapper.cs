using System.Linq;
using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordGuildMapper
{
    public static AppDiscordGuild Map(DiscordGuild guild)
    {
        var appGuild = new AppDiscordGuild()
        {
            Id = guild.Id,
            Name = guild.Name,
            Roles = guild.Roles.Values.Select(DiscordRoleMapper.Map).ToDictionary(x => x.Id),
            Channels = guild.Channels.Values.Select(DiscordChannelMapper.Map).ToDictionary(x => x.Id),
            Emojis = guild.Emojis.Values.Select(DiscordEmojiMapper.Map).ToDictionary(x => x.Id),
        };

        return appGuild;
    }
}

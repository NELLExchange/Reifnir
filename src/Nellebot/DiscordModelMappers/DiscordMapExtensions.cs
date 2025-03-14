using Nellebot.Common.AppDiscordModels;
using Nellebot.DiscordModelMappers;

// ReSharper disable once CheckNamespace = This is intentional
namespace DSharpPlus.Entities;

public static class DiscordMapExtensions
{
    public static AppDiscordGuild ToAppDiscordGuild(this DiscordGuild guild)
    {
        return DiscordGuildMapper.Map(guild);
    }
}

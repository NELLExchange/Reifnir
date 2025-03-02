using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordChannelMapper
{
    public static AppDiscordChannel Map(DiscordChannel discordChannel)
    {
        var channel = new AppDiscordChannel
        {
            Id = discordChannel.Id,
            Name = discordChannel.Name,
        };

        return channel;
    }
}

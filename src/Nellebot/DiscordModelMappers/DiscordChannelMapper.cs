using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordChannelMapper
{
    public static AppDiscordChannel Map(DiscordChannel discordChannel)
    {
        var channel = new AppDiscordChannel();

        channel.Id = discordChannel.Id;
        channel.Name = discordChannel.Name;

        return channel;
    }
}

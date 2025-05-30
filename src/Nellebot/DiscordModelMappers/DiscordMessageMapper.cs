﻿using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordMessageMapper
{
    public static AppDiscordMessage Map(DiscordMessage discordMessage)
    {
        var appDiscordMessage = new AppDiscordMessage
        {
            Content = discordMessage.Content,
            CreationTimestamp = discordMessage.CreationTimestamp,
        };

        if (discordMessage.Author is not null)
        {
            appDiscordMessage.Author = DiscordUserMapper.Map(discordMessage.Author);
        }

        if (discordMessage.Channel is not null)
        {
            appDiscordMessage.Channel = DiscordChannelMapper.Map(discordMessage.Channel);
        }

        return appDiscordMessage;
    }
}

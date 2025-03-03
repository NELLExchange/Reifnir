using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordEmojiMapper
{
    public static AppDiscordEmoji Map(DiscordEmoji emote)
    {
        return new AppDiscordEmoji
        {
            Id = emote.Id,
            Name = emote.Name,
            IsAnimated = emote.IsAnimated,
        };
    }
}

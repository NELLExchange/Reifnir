using System;
using DSharpPlus.Entities;

namespace Nellebot.Utils;

public static class EmbedBuilderHelper
{
    public static DiscordEmbed BuildSimpleEmbed(
        string message,
        int color = DiscordConstants.DefaultEmbedColor)
    {
        return BuildSimpleEmbed(string.Empty, message, color);
    }

    public static DiscordEmbed BuildSimpleEmbed(
        string title,
        string message,
        int color = DiscordConstants.DefaultEmbedColor)
    {
        string truncatedMessage = message[..Math.Min(message.Length, DiscordConstants.MaxEmbedContentLength)];

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
            .WithTitle(title)
            .WithDescription(truncatedMessage)
            .WithColor(color);

        return eb.Build();
    }
}

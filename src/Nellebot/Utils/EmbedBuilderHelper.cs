using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
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

    public static string EncodeUrlForDiscordEmbed(string url)
    {
        var uri = new Uri(url);
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
        string encodedQuery = string.Join(
            "&",
            query.AllKeys.Select(key => $"{key}={Uri.EscapeDataString(query[key])}"));

        string baseUrl = uri.GetLeftPart(UriPartial.Authority);
        if (uri.AbsolutePath != "/")
            baseUrl += uri.AbsolutePath;

        return $"{baseUrl}?{encodedQuery}";
    }
}

using System.Linq;
using System.Text.RegularExpressions;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.Utils;

public static class DiscordMentionEncoder
{
    private const string RoleEncodedFormat = "<@&{0}>";
    private const string ChannelEncodedFormat = "<#{0}>";
    private const string EmoteStaticEncodedFormat = "<:{0}:{1}>";
    private const string EmoteAnimatedEncodedFormat = "<a:{0}:{1}>";

    private const string RoleDecodedFormat = "@{0}";
    private const string ChannelDecodedFormat = "#{0}";
    private const string EmoteDecodedFormat = ":{0}:";

    private static readonly Regex RoleEncodedRegex = new(
        @"<@&(\d+)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ChannelEncodedRegex = new(
        @"<#(\d+)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmoteEncodedRegex = new(
        @"<a?:\w+:(\d+)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    ///    Encodes mentions in a string to their respective IDs.
    /// </summary>
    /// <param name="guild">The guild to use.</param>
    /// <param name="input">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    public static string EncodeMentions(AppDiscordGuild guild, string input)
    {
        AppDiscordRole[] allRoles = guild.Roles.Values.ToArray();
        AppDiscordChannel[] allChannels = guild.Channels.Values.ToArray();

        foreach (AppDiscordRole role in allRoles)
        {
            string roleString = string.Format(RoleDecodedFormat, role.Name);

            input = input.Replace(roleString, string.Format(RoleEncodedFormat, role.Id));
        }

        foreach (AppDiscordChannel channel in allChannels)
        {
            string channelString = string.Format(ChannelDecodedFormat, channel.Name);

            input = input.Replace(channelString, string.Format(ChannelEncodedFormat, channel.Id));
        }

        foreach (AppDiscordEmoji emote in guild.Emojis.Values)
        {
            string emoteString = string.Format(EmoteDecodedFormat, emote.Name);

            string encodedEmoteString = emote.IsAnimated
                ? string.Format(EmoteAnimatedEncodedFormat, emote.Name, emote.Id)
                : string.Format(EmoteStaticEncodedFormat, emote.Name, emote.Id);

            input = input.Replace(emoteString, encodedEmoteString);
        }

        return input;
    }

    /// <summary>
    ///     Decodes mentions in a string to their respective names.
    /// </summary>
    /// <param name="guild">The guild to decode mentions for.</param>
    /// <param name="input">The string to decode.</param>
    /// <returns>The decoded string.</returns>
    public static string DecodeMentions(AppDiscordGuild guild, string input)
    {
        MatchCollection roleMentions = RoleEncodedRegex.Matches(input);
        foreach (Match match in roleMentions)
        {
            if (!ulong.TryParse(match.Groups[1].Value, out ulong roleIdLong))
                continue;

            if (!guild.Roles.TryGetValue(roleIdLong, out AppDiscordRole? role))
                continue;

            string roleString = string.Format(RoleDecodedFormat, role.Name);
            input = input.Replace(match.Value, roleString);
        }

        MatchCollection channelMentions = ChannelEncodedRegex.Matches(input);
        foreach (Match match in channelMentions)
        {
            if (!ulong.TryParse(match.Groups[1].Value, out ulong channelIdLong))
                continue;

            if (!guild.Channels.TryGetValue(channelIdLong, out AppDiscordChannel? channel))
                continue;

            string channelString = string.Format(ChannelDecodedFormat, channel.Name);
            input = input.Replace(match.Value, channelString);
        }

        MatchCollection emoteMentions = EmoteEncodedRegex.Matches(input);
        foreach (Match match in emoteMentions)
        {
            if (!ulong.TryParse(match.Groups[1].Value, out ulong emoteIdLong))
                continue;

            if (!guild.Emojis.TryGetValue(emoteIdLong, out AppDiscordEmoji? emote))
                continue;

            string emoteString = string.Format(EmoteDecodedFormat, emote.Name);
            input = input.Replace(match.Value, emoteString);
        }

        return input;
    }
}

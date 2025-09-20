using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace Nellebot.Utils;

public static class DiscordExtensions
{
    public static string GetDetailedMemberIdentifier(this DiscordMember member, bool useMention = false)
    {
        string memberUsername = member.Username;
        string memberDisplayName = member.DisplayName;
        string mentionOrDisplayName = useMention ? member.Mention : memberDisplayName;

        string memberFormattedDisplayName = memberUsername != memberDisplayName
            ? $"{mentionOrDisplayName} ({member.GetFullUsername()}, {member.Id})"
            : $"{member.GetFullUsername()} ({member.Id})";

        return memberFormattedDisplayName;
    }

    public static string GetFullUsername(this DiscordUser user)
    {
        return user.HasLegacyUsername() ? $"{user.Username}#{user.Discriminator}" : user.Username;
    }

    private static bool HasLegacyUsername(this DiscordUser user)
    {
        return user.Discriminator != "0";
    }

    public static Task CreateSuccessReactionAsync(this DiscordMessage message)
    {
        return message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
    }

    public static Task CreateFailureReactionAsync(this DiscordMessage message)
    {
        return message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
    }

    public static string GetQuotedContent(this DiscordMessage message)
    {
        List<string> lines = message.Content.Split(DiscordConstants.NewLineChar).ToList();

        IEnumerable<string> quotedLines = lines.Select(line => $"> {line}");

        return string.Join(DiscordConstants.NewLineChar, quotedLines);
    }

    public static string NullOrWhiteSpaceTo(this string? input, string fallback)
    {
        return !string.IsNullOrWhiteSpace(input) ? input : fallback;
    }

    public static void ThrowIfNull([NotNull] this DiscordGuild? guild)
    {
        _ = guild ?? throw new InvalidOperationException("Guild null null");
    }

    public static bool IsImageAttachment(this DiscordAttachment attachment)
    {
        var imageExtensions = new[] { "png", "jpg", "jpeg", "gif", "webp" };
        return attachment.MediaType?.StartsWith("image")
               ?? imageExtensions.Any(ext => (attachment.FileName ?? string.Empty).EndsWith($".{ext}"));
    }

    public static Task<DiscordMessage> SendSuppressedMessageAsync(this DiscordChannel channel, string content)
    {
        return channel.SendMessageAsync(x => { x.WithContent(content).SuppressNotifications(); });
    }

    public static Task<DiscordMessage> SendSuppressedMessageAsync(this DiscordChannel channel, DiscordEmbed embed)
    {
        return channel.SendMessageAsync(x => { x.AddEmbed(embed).SuppressNotifications(); });
    }

    public static Task<DiscordMessage> SendSuppressedMessageAsync(
        this DiscordChannel channel,
        string content,
        DiscordEmbed embed)
    {
        return channel.SendMessageAsync(x => { x.WithContent(content).AddEmbed(embed).SuppressNotifications(); });
    }

    public static Task<DiscordMessage> SendSuppressedMessageAsync(
        this DiscordChannel channel,
        DiscordMessageBuilder builder)
    {
        return channel.SendMessageAsync(builder.SuppressNotifications());
    }

    public static bool IsUserAssignable(this DiscordRole role)
    {
        return role.Flags.HasFlag(DiscordRoleFlags.InPrompt);
    }

    public static async Task TryRespondEphemeral(
        this CommandContext ctx,
        string successMessage,
        DiscordInteraction? modalInteraction)
    {
        if (ctx is SlashCommandContext slashCtx)
        {
            if (modalInteraction is null)
            {
                await slashCtx.RespondAsync(successMessage, ephemeral: true);
            }
            else
            {
                DiscordFollowupMessageBuilder followupBuilder = new DiscordFollowupMessageBuilder()
                    .WithContent(successMessage)
                    .AsEphemeral();

                await modalInteraction.CreateFollowupMessageAsync(followupBuilder);
            }
        }
        else
        {
            await ctx.RespondAsync(successMessage);
        }
    }

    public static Task TryRespondEphemeral(
        this CommandContext ctx,
        string successMessage)
    {
        return Task.FromResult(ctx.TryRespondEphemeral(successMessage, modalInteraction: null));
    }
}

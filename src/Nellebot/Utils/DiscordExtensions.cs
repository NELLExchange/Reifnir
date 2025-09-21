using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

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

    /// <summary>
    ///     Retrieves a typed value from modal submission results.
    /// </summary>
    /// <typeparam name="TResult">The expected type of the value.</typeparam>
    /// <param name="modalResult">The modal submission event arguments.</param>
    /// <param name="id">The component ID to retrieve the value for.</param>
    /// <returns>The typed value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the component ID is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be cast to TResult.</exception>
    /// <exception cref="NotSupportedException">Thrown when the modal submission type is not supported.</exception>
    public static TResult GetValue<TResult>(this ModalSubmittedEventArgs modalResult, string id)
    {
        if (!modalResult.Values.TryGetValue(id, out IModalSubmission? submission))
        {
            throw new InvalidOperationException($"Modal submission value with id '{id}' not found");
        }

        const string invalidCastExceptionString = "Cannot cast modal result to {0}";

        return submission switch
        {
            ChannelSelectMenuModalSubmission channelSelectSubmission =>
                channelSelectSubmission.Ids is not TResult channelIds
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : channelIds,
            MentionableSelectMenuModalSubmission mentionableSelectSubmission =>
                mentionableSelectSubmission.Ids is not TResult mentionableIds
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : mentionableIds,
            RoleSelectMenuModalSubmission roleSelectSubmission =>
                roleSelectSubmission.Ids is not TResult roleIds
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : roleIds,
            SelectMenuModalSubmission selectMenuSubmission =>
                selectMenuSubmission.Values is not TResult selectValues
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : selectValues,
            TextInputModalSubmission textInputSubmission =>
                textInputSubmission.Value is not TResult textValue
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : textValue,
            UserSelectMenuModalSubmission userSelectSubmission =>
                userSelectSubmission.Ids is not TResult userIds
                    ? throw new InvalidCastException(string.Format(invalidCastExceptionString, typeof(TResult).Name))
                    : userIds,
            _ => throw new NotSupportedException($"Unsupported modal submission type: {submission.GetType().Name}"),
        };
    }

    /// <summary>
    ///     Attempts to retrieve a typed value from modal submission results.
    /// </summary>
    /// <typeparam name="TResult">The expected type of the value.</typeparam>
    /// <param name="modalResult">The modal submission event arguments.</param>
    /// <param name="id">The component ID to retrieve the value for.</param>
    /// <param name="value">
    ///     When this method returns, contains the typed value if found and castable; otherwise, the default
    ///     value for the type.
    /// </param>
    /// <returns>true if the value was found and successfully cast; otherwise, false.</returns>
    public static bool TryGetValue<TResult>(this ModalSubmittedEventArgs modalResult, string id, out TResult? value)
    {
        if (!modalResult.Values.TryGetValue(id, out IModalSubmission? submission))
        {
            value = default;
            return false;
        }

        value = submission switch
        {
            ChannelSelectMenuModalSubmission channelSelectSubmission =>
                channelSelectSubmission.Ids is TResult channelIds ? channelIds : default,
            MentionableSelectMenuModalSubmission mentionableSelectSubmission =>
                mentionableSelectSubmission.Ids is TResult mentionableIds ? mentionableIds : default,
            RoleSelectMenuModalSubmission roleSelectSubmission =>
                roleSelectSubmission.Ids is TResult roleIds ? roleIds : default,
            SelectMenuModalSubmission selectMenuSubmission =>
                selectMenuSubmission.Values is TResult selectValues ? selectValues : default,
            TextInputModalSubmission textInputSubmission =>
                textInputSubmission.Value is TResult textValue ? textValue : default,
            UserSelectMenuModalSubmission userSelectSubmission =>
                userSelectSubmission.Ids is TResult userIds ? userIds : default,
            _ => default,
        };

        return value is not null;
    }
}

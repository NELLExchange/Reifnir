using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Services;

public class AwardMessageService
{
    private const string SpoilerImageAttachmentPrefix = "SPOILER_";
    private const string InvisibleChar = "‎ ";
    private readonly AwardMessageRepository _awardMessageRepo;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly ILogger<AwardMessageService> _logger;
    private readonly BotOptions _options;

    public AwardMessageService(
        AwardMessageRepository awardMessageRepo,
        ILogger<AwardMessageService> logger,
        IOptions<BotOptions> options,
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver)
    {
        _awardMessageRepo = awardMessageRepo;
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _options = options.Value;
    }

    public async Task HandleAwardChange(DiscordMessage partialMessage)
    {
        DiscordChannel channel = partialMessage.Channel ?? throw new Exception("Channel was null");
        DiscordMessage? message = await _discordResolver.ResolveMessage(channel, partialMessage.Id);
        DiscordGuild guild = channel.Guild;

        if (message == null)
        {
            _logger.LogDebug("Could not resolve message");
            return;
        }

        if (message.Author is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        DiscordMember? messageAuthor = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        if (messageAuthor is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        DiscordChannel? awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        uint awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

        bool hasEnoughAwards = awardReactionCount >= _options.RequiredAwardCount;

        AwardMessage? awardMessage =
            await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, message.Id);

        _logger.LogDebug($"Message has {awardReactionCount} awards");

        if (awardMessage == null)
        {
            _logger.LogDebug($"Message ({message.Id}) does not exist in the database");

            if (!hasEnoughAwards)
            {
                _logger.LogDebug($"Not enough awards. {awardReactionCount} / {_options.RequiredAwardCount}");
                return;
            }

            DiscordMessage postedMessage = await PostAwardedMessage(
                awardChannel,
                message,
                messageAuthor,
                awardReactionCount);

            await _awardMessageRepo.CreateAwardMessage(
                message.Id,
                message.ChannelId,
                postedMessage.Id,
                awardChannel.Id,
                messageAuthor.Id,
                awardReactionCount);
        }
        else
        {
            _logger.LogDebug("Message ({messageId}) exists in award channel", message.Id);

            // TODO keep track if message was removed from award channels
            // so it's handled gracefully i.e. not throw an error
            // when it tries to update a removed message
            await UpdateAwardedMessageText(awardChannel, awardMessage.AwardedMessageId, awardReactionCount);

            await _awardMessageRepo.UpdateAwardCount(awardMessage.Id, awardReactionCount);
        }
    }

    public async Task HandleAwardMessageUpdated(DiscordMessage partialMessage)
    {
        DiscordChannel channel = partialMessage.Channel ?? throw new Exception("Channel was null");
        DiscordMessage? message = await _discordResolver.ResolveMessage(channel, partialMessage.Id);
        DiscordGuild guild = channel.Guild;

        if (message == null)
        {
            _logger.LogDebug("Could not resolve message");
            return;
        }

        if (message.Author is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        DiscordMember? messageAuthor = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        if (messageAuthor is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        DiscordChannel? awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        uint awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

        bool hasAwards = awardReactionCount > 0;

        if (!hasAwards)
        {
            _logger.LogDebug("Updated message has no awards");
            return;
        }

        AwardMessage? awardMessage =
            await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, message.Id);

        if (awardMessage == null)
        {
            _logger.LogDebug($"Could not find ({message.Id}) in the database");
            return;
        }

        await UpdateAwardedMessageEmbed(awardChannel, awardMessage.AwardedMessageId, message, messageAuthor);
    }

    public async Task HandleAwardMessageDeleted(ulong messageId)
    {
        DiscordChannel? awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        AwardMessage? awardMessage =
            await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, messageId);

        if (awardMessage == null)
        {
            _logger.LogDebug($"Message ({messageId}) does not exist in award channel");
            return;
        }

        await DeleteAwardedMessage(awardChannel, awardMessage.AwardedMessageId);

        await _awardMessageRepo.DeleteAwardMessage(awardMessage.Id);
    }

    public async Task HandleAwardedMessageDeleted(ulong messageId)
    {
        DiscordChannel? awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        AwardMessage? awardMessage =
            await _awardMessageRepo.GetAwardMessageByAwardedMessageId(awardChannel.Id, messageId);

        if (awardMessage is null)
        {
            _logger.LogDebug($"Message ({messageId}) does not exist in award channel");
            return;
        }

        await _awardMessageRepo.DeleteAwardMessage(awardMessage.Id);
    }

    private async Task<DiscordMessage> PostAwardedMessage(
        DiscordChannel awardChannel,
        DiscordMessage originalMessage,
        DiscordMember author,
        uint awardCount)
    {
        DiscordChannel messageChannel = originalMessage.Channel ?? throw new Exception("Channel was null");

        DiscordEmbed embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

        var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

        var messageText = $"{awardText}";

        return await awardChannel.SendMessageAsync(messageText, embed);
    }

    private async Task UpdateAwardedMessageText(
        DiscordChannel awardChannel,
        ulong awardedMessageId,
        uint awardCount)
    {
        DiscordMessage? awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

        var messageText = $"{awardText}";

        await awardedMessage.ModifyAsync(messageText);
    }

    private async Task UpdateAwardedMessageEmbed(
        DiscordChannel awardChannel,
        ulong awardedMessageId,
        DiscordMessage originalMessage,
        DiscordMember author)
    {
        DiscordChannel messageChannel = originalMessage.Channel ?? throw new Exception("Channel was null");

        DiscordMessage? awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        DiscordEmbed embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

        await awardedMessage.ModifyAsync(embed);
    }

    private async Task DeleteAwardedMessage(
        DiscordChannel awardChannel,
        ulong awardedMessageId)
    {
        DiscordMessage? awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        await awardedMessage.DeleteAsync();
    }

    private DiscordEmbed BuildAwardedMessageEmbed(DiscordMessage message, DiscordMember author, string channel)
    {
        string authorDisplayName = author.DisplayName;

        var messageContentSb = new StringBuilder();

        messageContentSb.AppendLine($"[**Jump to message!**]({message.JumpLink})");

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            messageContentSb.AppendLine();
            messageContentSb.AppendLine(message.Content);
        }

        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            .WithAuthor(authorDisplayName, null, author.GuildAvatarUrl ?? author.AvatarUrl)
            .WithFooter($"#{channel}")
            .WithTimestamp(message.Id)
            .WithColor(DiscordConstants.DefaultEmbedColor);

        if (message.Attachments.Count > 0)
        {
            var addedEmbedImage = false;

            foreach (DiscordAttachment attachment in message.Attachments)
            {
                switch (attachment.MediaType)
                {
                    case { } s when s.StartsWith("video"):
                        messageContentSb.AppendLine();
                        messageContentSb.AppendLine($"`Video attachment: {attachment.FileName}`");
                        break;
                    case { } s when s.StartsWith("image"):
                        if (attachment.FileName is null)
                        {
                            messageContentSb.AppendLine();
                            messageContentSb.AppendLine("`Couldn't load image.`");
                            break;
                        }

                        if (attachment.FileName.StartsWith(SpoilerImageAttachmentPrefix))
                        {
                            messageContentSb.AppendLine();
                            messageContentSb.AppendLine("`Spoiler image hidden. Use the jump link to view it.`");
                        }
                        else if (!addedEmbedImage && attachment.Url is not null)
                        {
                            embedBuilder = embedBuilder.WithImageUrl(attachment.Url);
                            addedEmbedImage = true;
                        }

                        break;
                    default:
                        messageContentSb.AppendLine();
                        messageContentSb.AppendLine($"`File attachment: {attachment.FileName}`");
                        break;
                }
            }
        }
        else
        {
            // Try to fetch an image from message embed
            DiscordEmbed? messageEmbed = message.Embeds.FirstOrDefault();

            if (messageEmbed != null)
            {
                if (messageEmbed.Thumbnail?.ProxyUrl.HasValue ?? false)
                {
                    embedBuilder = embedBuilder.WithImageUrl(messageEmbed.Thumbnail.ProxyUrl.Value.ToUri()!);
                }

                if (!string.IsNullOrWhiteSpace(messageEmbed.Title) && messageEmbed.Url != null)
                {
                    var embedLinkText = $"[{messageEmbed.Title}]({messageEmbed.Url})";

                    messageContentSb.AppendLine();
                    messageContentSb.AppendLine(embedLinkText);
                }
            }
        }

        string messageContent = messageContentSb.ToString().Replace(" ", $"{InvisibleChar}");

        embedBuilder = embedBuilder.WithDescription(messageContent);

        DiscordEmbed embed = embedBuilder.Build();

        return embed;
    }

    private async Task<uint> GetAwardReactionCount(
        DiscordMessage message,
        DiscordMember messageAuthor)
    {
        uint awardReactionCount = 0;

        try
        {
            DiscordEmoji cookieEmoji = DiscordEmoji.FromUnicode(EmojiMap.Cookie);

            List<DiscordUser> cookieReactionUsers =
                await message.GetReactionsAsync(cookieEmoji).ToListAsync();

            // ReSharper disable once RedundantAssignment
            var skipAuthor = false;
#if DEBUG
            skipAuthor = true;
#endif
            awardReactionCount = (uint)cookieReactionUsers
                .Select(u => u.Id)
                .Distinct()
                .Count(x => skipAuthor || x != messageAuthor.Id);
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, nameof(GetAwardReactionCount));
        }

        return awardReactionCount;
    }
}

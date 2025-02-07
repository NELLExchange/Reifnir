using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class AwardsHandler : INotificationHandler<MessageReactionAddedNotification>,
    INotificationHandler<MessageReactionRemovedNotification>,
    INotificationHandler<MessageUpdatedNotification>,
    INotificationHandler<MessageDeletedNotification>
{
    private readonly AwardMessageService _awardMessageService;
    private readonly ILogger<AwardsHandler> _logger;
    private readonly BotOptions _options;

    public AwardsHandler(
        ILogger<AwardsHandler> logger,
        IOptions<BotOptions> options,
        AwardMessageService awardMessageService)
    {
        _logger = logger;
        _awardMessageService = awardMessageService;
        _options = options.Value;
    }

    public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageDeletedEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        ulong messageId = eventArgs.Message.Id;

        if (channel.IsPrivate) return;

        if (IsAwardAllowedChannel(channel))
        {
            await _awardMessageService.HandleAwardMessageDeleted(messageId);
        }
        else if (IsAwardChannel(channel))
        {
            await _awardMessageService.HandleAwardedMessageDeleted(messageId);
        }
    }

    public async Task Handle(MessageReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionAddedEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordUser user = eventArgs.User;
        DiscordEmoji emoji = eventArgs.Emoji;

        if (!ShouldHandleReaction(channel, user)) return;

        bool isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardMessageService.HandleAwardChange(message);
    }

    public async Task Handle(MessageReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionRemovedEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordUser user = eventArgs.User;
        DiscordEmoji emoji = eventArgs.Emoji;

        if (!ShouldHandleReaction(channel, user)) return;

        bool isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardMessageService.HandleAwardChange(message);
    }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        MessageUpdatedEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage? message = eventArgs.Message;
        DiscordUser user = eventArgs.Author;

        if (message == null) throw new Exception($"{nameof(eventArgs.Message)} is null");

        if (!ShouldHandleReaction(channel, user)) return;

        if (!IsAwardAllowedChannel(channel)) return;

        await _awardMessageService.HandleAwardMessageUpdated(message);
    }

    /// <summary>
    ///     Don't care about private messages
    ///     Don't care about bot reactions.
    /// </summary>
    private static bool ShouldHandleReaction(DiscordChannel? channel, DiscordUser? author)
    {
        // This seems to happen because of the newly introduced Threads feature
        if (author is null) return false;

        if (author.IsBot || (author.IsSystem ?? false)) return false;

        return !channel?.IsPrivate ?? false;
    }

    private bool IsAwardAllowedChannel(DiscordChannel channel)
    {
        if (channel.IsThread) channel = channel.Parent;

        ulong[] allowedGroupIds = _options.AwardVoteGroupIds;

        if (allowedGroupIds.Length == 0)
        {
            _logger.LogDebug($"{nameof(_options.AwardVoteGroupIds)} is empty");
            return false;
        }

        bool isAllowedChannel = allowedGroupIds.ToList().Contains(channel.ParentId!.Value);

        return isAllowedChannel;
    }

    private bool IsAwardChannel(DiscordChannel channel)
    {
        ulong awardChannelId = _options.AwardChannelId;

        return channel.Id == awardChannelId;
    }
}

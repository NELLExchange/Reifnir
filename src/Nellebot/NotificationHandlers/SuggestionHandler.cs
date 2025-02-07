using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class SuggestionHandler : INotificationHandler<MessageCreatedNotification>
{
    private readonly BotOptions _options;

    public SuggestionHandler(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        ulong suggestionsForumChannelId = _options.SuggestionsChannelId;

        DiscordChannel channel = notification.EventArgs.Channel;
        DiscordMessage message = notification.EventArgs.Message;

        ulong? channelParentId = channel.Parent?.Id;

        if (channelParentId == null || channelParentId != suggestionsForumChannelId) return;

        bool isOriginalForumPost = message.Id == channel!.Id;

        if (!isOriginalForumPost) return;

        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUp));
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowDown));
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUpDown));
    }
}

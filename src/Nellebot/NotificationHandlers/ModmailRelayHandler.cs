using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Utils;
using Nellebot.Workers;
using BaseContext = Nellebot.CommandHandlers.BaseContext;

namespace Nellebot.NotificationHandlers;

public class ModmailRelayHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string CancelMessageToken = "cancel";
    private readonly BotOptions _botOptions;

    private readonly CommandParallelQueueChannel _commandQueue;
    private readonly ModmailTicketRepository _modmailTicketRepo;

    public ModmailRelayHandler(
        CommandParallelQueueChannel commandQueue,
        ModmailTicketRepository modmailTicketRepo,
        IOptions<BotOptions> botOptions)
    {
        _commandQueue = commandQueue;
        _modmailTicketRepo = modmailTicketRepo;
        _botOptions = botOptions.Value;
    }

    public Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        MessageCreatedEventArgs args = notification.EventArgs;

        DiscordChannel channel = args.Channel;
        DiscordUser user = args.Author;
        DiscordMessage message = args.Message;

        if (user.IsBot) return Task.CompletedTask;

        if (channel.IsPrivate) return HandlePrivateMessage(channel, user, message, cancellationToken);

        if (channel.ParentId == _botOptions.ModmailChannelId)
        {
            return HandleThreadMessage(channel, user, message, cancellationToken);
        }

        // It's a non-private message in a random-ass channel
        return Task.CompletedTask;
    }

    private async Task HandlePrivateMessage(
        DiscordChannel channel,
        DiscordUser user,
        DiscordMessage message,
        CancellationToken cancellationToken)
    {
        // The message could be an interactivity response containing the token "cancel".
        // If so, disregard the message. Not the most elegant solution, but it should do.
        if (message.Content.Equals(CancelMessageToken, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        ModmailTicket? userTicketInPool =
            await _modmailTicketRepo.GetActiveTicketByRequesterId(user.Id, cancellationToken);

        if (userTicketInPool == null)
        {
            var baseContext = new BaseContext
            {
                Channel = channel,
                User = user,
            };

            var requestTicketCommand = new RequestModmailTicketCommand(baseContext, message);

            await _commandQueue.Writer.WriteAsync(requestTicketCommand, cancellationToken);

            return;
        }

        // User is still in the process of requesting a ticket
        if (userTicketInPool.IsStub) return;

        var requesterMessageContext = new MessageContext
        {
            Channel = channel,
            User = user,
            Message = message,
        };

        var relayRequesterMessageCommand = new RelayRequesterMessageCommand(requesterMessageContext, userTicketInPool);

        await _commandQueue.Writer.WriteAsync(relayRequesterMessageCommand, cancellationToken);
    }

    private async Task HandleThreadMessage(
        DiscordChannel channel,
        DiscordUser user,
        DiscordMessage message,
        CancellationToken cancellationToken)
    {
        ModmailTicket? channelTicketInPool =
            await _modmailTicketRepo.GetTicketByChannelId(channel.Id, cancellationToken);

        if (channelTicketInPool == null)
        {
            await message.CreateFailureReactionAsync();

            return;
        }

        var moderatorMessageContext = new MessageContext
        {
            Channel = channel,
            User = user,
            Message = message,
        };

        var relayModeratorMessageCommand =
            new RelayModeratorMessageCommand(moderatorMessageContext, channelTicketInPool);

        await _commandQueue.Writer.WriteAsync(relayModeratorMessageCommand, cancellationToken);
    }
}

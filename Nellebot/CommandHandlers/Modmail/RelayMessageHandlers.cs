﻿using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Data.Repositories;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class RelayMessageHandlers : IRequestHandler<RelayRequesterMessageCommand>,
                                    IRequestHandler<RelayModeratorMessageCommand>
{
    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;
    private readonly ModmailTicketRepository _modmailTicketRepo;

    public RelayMessageHandlers(IOptions<BotOptions> options, DiscordResolver resolver, ModmailTicketRepository modmailTicketRepo)
    {
        _options = options.Value;
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
    }

    /// <summary>
    /// Relay requester's message to the modmail thread.
    /// </summary>
    /// <param name="request">The <see cref="RelayRequesterMessageCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Handle(RelayRequesterMessageCommand request, CancellationToken cancellationToken)
    {
        var ticket = request.Ticket;
        var messageToRelay = request.Ctx.Message;

        var ticketPost = ticket.TicketPost
            ?? throw new Exception("The ticket does not have a post channelId");

        var threadChannel = _resolver.ResolveThread(ticketPost.ChannelThreadId)
            ?? throw new Exception("Could not resolve thread channel");

        var relayMessageContent = $"""
            {ticket.RequesterDisplayName} says
            {messageToRelay.GetQuotedContent()}
            """;

        await threadChannel.SendMessageAsync(relayMessageContent);

        await messageToRelay.CreateSuccessReactionAsync();

        _ = await _modmailTicketRepo.RefreshTicketLastActivity(ticket, cancellationToken);
    }

    /// <summary>
    /// Relay moderator's message as a dm to the requester.
    /// </summary>
    /// <param name="request">The <see cref="RelayModeratorMessageCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Handle(RelayModeratorMessageCommand request, CancellationToken cancellationToken)
    {
        var moderatorMember = (await _resolver.ResolveGuildMember(request.Ctx.User.Id))
                                ?? throw new Exception("Could not resolve member");

        var messageToRelay = request.Ctx.Message;

        if (!moderatorMember.Roles.Any(r => r.Id == _options.AdminRoleId))
        {
            await messageToRelay.CreateFailureReactionAsync();
            return;
        }

        var relayMessageContent = $"""
            Message from moderator:
            {messageToRelay.GetQuotedContent()}
            """;

        var requesterMember = (await _resolver.ResolveGuildMember(request.Ticket.RequesterId))
                                ?? throw new Exception("Could not resolve member");

        var relayedMessage = await requesterMember.SendMessageAsync(relayMessageContent);

        await messageToRelay.CreateSuccessReactionAsync();

        _ = await _modmailTicketRepo.RefreshTicketLastActivity(request.Ticket, cancellationToken);
    }
}

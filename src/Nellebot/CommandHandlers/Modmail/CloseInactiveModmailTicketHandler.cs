using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class CloseInactiveModmailTicketHandler : IRequestHandler<CloseInactiveModmailTicketCommand>
{
    private readonly ModmailTicketRepository _modmailTicketRepo;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _resolver;

    public CloseInactiveModmailTicketHandler(
        DiscordResolver resolver,
        ModmailTicketRepository modmailTicketRepo,
        IDiscordErrorLogger discordErrorLogger)
    {
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
        _discordErrorLogger = discordErrorLogger;
    }

    public async Task Handle(CloseInactiveModmailTicketCommand request, CancellationToken cancellationToken)
    {
        ModmailTicket ticket = request.Ticket;

        ModmailTicketPost ticketPost = ticket.TicketPost
                                       ?? throw new Exception("The ticket does not have a post channelId");

        await _modmailTicketRepo.CloseTicket(ticket, cancellationToken);

        DiscordThreadChannel? threadChannel = _resolver.ResolveThread(ticketPost.ChannelThreadId);

        if (threadChannel is null)
        {
            _discordErrorLogger.LogWarning(
                $"Could not resolve thread channel for ticket id: {ticket.Id}",
                $"Thread channel for ticket id: {ticket.Id} could not be resolved. Probably deleted. Closing ticket anyway.");
        }
        else
        {
            const string ticketClosureMessage = "This ticket has been closed due to inactivity.";

            await threadChannel.SendMessageAsync(ticketClosureMessage);
        }
    }
}

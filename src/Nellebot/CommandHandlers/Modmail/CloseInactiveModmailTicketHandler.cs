using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class CloseInactiveModmailTicketHandler : IRequestHandler<CloseInactiveModmailTicketCommand>
{
    private readonly ModmailTicketRepository _modmailTicketRepo;
    private readonly DiscordResolver _resolver;

    public CloseInactiveModmailTicketHandler(DiscordResolver resolver, ModmailTicketRepository modmailTicketRepo)
    {
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
    }

    public async Task Handle(CloseInactiveModmailTicketCommand request, CancellationToken cancellationToken)
    {
        ModmailTicket ticket = request.Ticket;

        ModmailTicketPost ticketPost = ticket.TicketPost
                                       ?? throw new Exception("The ticket does not have a post channelId");

        DiscordThreadChannel threadChannel = _resolver.ResolveThread(ticketPost.ChannelThreadId)
                                             ?? throw new Exception("Could not resolve thread channel");

        await _modmailTicketRepo.CloseTicket(ticket, cancellationToken);

        const string ticketClosureMessage = "This ticket has been closed due to inactivity.";

        await threadChannel.SendMessageAsync(ticketClosureMessage);
    }
}

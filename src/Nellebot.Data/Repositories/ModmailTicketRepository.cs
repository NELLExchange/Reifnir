using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.Data.Repositories;

public class ModmailTicketRepository
{
    private readonly BotDbContext _dbContext;

    public ModmailTicketRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModmailTicket?> GetActiveTicketByRequesterId(
        ulong requesterId,
        CancellationToken cancellationToken = default)
    {
        List<ModmailTicket> allActiveTickets = await _dbContext.ModmailTickets
            .Where(x => !x.IsClosed)
            .ToListAsync(cancellationToken);

        return allActiveTickets.SingleOrDefault(x => x.RequesterId == requesterId);
    }

    public Task<ModmailTicket?> GetTicketByChannelId(ulong channelId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ModmailTickets
            .SingleOrDefaultAsync(
                x => x.TicketPost != null && x.TicketPost.ChannelThreadId == channelId,
                cancellationToken);
    }

    public async Task<ModmailTicket> CreateTicket(ModmailTicket ticket, CancellationToken cancellationToken = default)
    {
        bool activeTicketExists = await _dbContext.ModmailTickets
            .AnyAsync(x => x.RequesterId == ticket.RequesterId && !x.IsClosed, cancellationToken);

        if (activeTicketExists)
        {
            throw new Exception($"An active ticket already exists for requester {ticket.RequesterId}");
        }

        _dbContext.ModmailTickets.Add(ticket);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ticket;
    }

    public async Task DeleteTicket(ModmailTicket ticket, CancellationToken cancellationToken = default)
    {
        ModmailTicket existingTicket =
            await _dbContext.ModmailTickets.FindAsync(new object[] { ticket.Id }, cancellationToken)
            ?? throw new Exception($"Could not find ticket with id {ticket.Id}");

        _dbContext.ModmailTickets.Remove(existingTicket);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ModmailTicket> RefreshTicketLastActivity(
        ModmailTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ModmailTicket existingTicket =
            await _dbContext.ModmailTickets.FindAsync(new object[] { ticket.Id }, cancellationToken)
            ?? throw new Exception($"Could not find ticket with id {ticket.Id}");

        existingTicket.LastActivity = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingTicket;
    }

    public async Task<ModmailTicket> UpdateTicketPost(
        ModmailTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ModmailTicket existingTicket =
            await _dbContext.ModmailTickets.FindAsync(new object[] { ticket.Id }, cancellationToken)
            ?? throw new Exception($"Could not find ticket with id {ticket.Id}");

        existingTicket.TicketPost = ticket.TicketPost;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingTicket;
    }

    public async Task<ModmailTicket> CloseTicket(ModmailTicket ticket, CancellationToken cancellationToken = default)
    {
        ModmailTicket existingTicket =
            await _dbContext.ModmailTickets.FindAsync(new object[] { ticket.Id }, cancellationToken)
            ?? throw new Exception($"Could not find ticket with id {ticket.Id}");

        existingTicket.IsClosed = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existingTicket;
    }

    public Task<List<ModmailTicket>> GetOpenExpiredTickets(TimeSpan expiryThreshold)
    {
        var removed = new List<ModmailTicket>();

        return _dbContext.ModmailTickets
            .Where(x => DateTime.UtcNow - x.LastActivity > expiryThreshold && !x.IsClosed)
            .ToListAsync();
    }
}

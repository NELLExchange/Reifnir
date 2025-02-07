using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    public Guid Id { get; set; }

    public required ulong RequesterId { get; set; }

    public required string RequesterDisplayName { get; set; }

    public required bool IsAnonymous { get; set; }

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public ModmailTicketPost? TicketPost { get; set; }

    public bool IsClosed { get; set; }

    public bool IsStub => TicketPost == null;
}

public record ModmailTicketPost(ulong ChannelThreadId, ulong MessageId);

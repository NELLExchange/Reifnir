using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.CommandHandlers.Modmail;

public record RequestModmailTicketCommand : BaseCommand
{
    public RequestModmailTicketCommand(CommandContext ctx)
        : base(BaseContext.FromCommandContext(ctx))
    { }

    public RequestModmailTicketCommand(BaseContext ctx, DiscordMessage requestMessage)
        : base(ctx)
    {
        RequestMessage = requestMessage;
    }

    public DiscordMessage? RequestMessage { get; init; }
}

public record RelayModeratorMessageCommand(MessageContext Ctx, ModmailTicket Ticket) : MessageCommand(Ctx);

public record RelayRequesterMessageCommand(MessageContext Ctx, ModmailTicket Ticket) : MessageCommand(Ctx);

public record CloseInactiveModmailTicketCommand(ModmailTicket Ticket) : ICommand;

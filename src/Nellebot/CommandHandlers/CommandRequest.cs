using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using MediatR;

namespace Nellebot.CommandHandlers;

public interface ICommand : IRequest
{ }

public interface IQuery : IRequest
{ }

public record BaseCommand : ICommand
{
    protected BaseCommand(BaseContext ctx)
    {
        Ctx = ctx;
    }

    public BaseContext Ctx { get; }
}

public record MessageCommand : ICommand
{
    protected MessageCommand(MessageContext ctx)
    {
        Ctx = ctx;
    }

    public MessageContext Ctx { get; }
}

public record BotSlashCommand : BotCommandCommand
{
    protected BotSlashCommand(SlashCommandContext ctx)
        : base(ctx)
    {
        Ctx = ctx;
    }

    public new SlashCommandContext Ctx { get; }
}

public record BotCommandCommand : ICommand
{
    protected BotCommandCommand(CommandContext ctx)
    {
        Ctx = ctx;
    }

    public CommandContext Ctx { get; }
}

public record BotCommandQuery : IQuery
{
    protected BotCommandQuery(CommandContext ctx)
    {
        Ctx = ctx;
    }

    public CommandContext Ctx { get; }
}

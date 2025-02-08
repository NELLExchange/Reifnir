using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.MessageTemplates;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Messages;

[BaseCommandCheck]
[RequireTrustedMember]
[Command("goodbye-msg")]
public class GoodbyeMessageModule
{
    private readonly CommandParallelQueueChannel _commandParallelQueue;
    private readonly CommandQueueChannel _commandQueue;

    public GoodbyeMessageModule(CommandQueueChannel commandQueue, CommandParallelQueueChannel commandParallelQueue)
    {
        _commandQueue = commandQueue;
        _commandParallelQueue = commandParallelQueue;
    }

    [Command("add")]
    public async Task AddGoodbyeMessage(CommandContext ctx, [RemainingText] string message)
    {
        await _commandQueue.Writer.WriteAsync(new AddGoodbyeMessageCommand(ctx, message));
    }

    [Command("remove")]
    public async Task RemoveGoodbyeMessage(CommandContext ctx, [RemainingText] string messageId)
    {
        await _commandQueue.Writer.WriteAsync(new DeleteGoodbyeMessageCommand(ctx, messageId));
    }

    [Command("list")]
    public async Task GetGoodbyeMessages(CommandContext ctx)
    {
        await _commandParallelQueue.Writer.WriteAsync(new GetGoodbyeMessagesCommand(ctx));
    }
}

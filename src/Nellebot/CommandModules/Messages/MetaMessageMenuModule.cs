using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.MessageTemplates;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Messages;

public class MetaMessageMenuModule
{
    private readonly CommandParallelQueueChannel _commandQueue;

    public MetaMessageMenuModule(CommandParallelQueueChannel commandQueue)
    {
        _commandQueue = commandQueue;
    }

    [BaseCommandCheck]
    [RequireTrustedMember]
    [Command("Add meta message after")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task AddMessage(SlashCommandContext ctx, DiscordMessage message)
    {
        await _commandQueue.Writer.WriteAsync(new AddMetaMessageCommand(ctx, ctx.Channel, message));
    }

    [Command("Edit meta message")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task EditMessage(SlashCommandContext ctx, DiscordMessage message)
    {
        await _commandQueue.Writer.WriteAsync(new EditMetaMessageCommand(ctx, message));
    }
}

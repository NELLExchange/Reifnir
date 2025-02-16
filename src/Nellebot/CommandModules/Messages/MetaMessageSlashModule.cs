using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.MessageTemplates;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Messages;

[BaseCommandCheck]
[RequireTrustedMember]
[Command("meta-msg")]
[Description("Manage meta messages")]
public class MetaMessageSlashModule
{
    private readonly CommandParallelQueueChannel _commandQueue;

    public MetaMessageSlashModule(CommandParallelQueueChannel commandQueue)
    {
        _commandQueue = commandQueue;
    }

    [Command("add")]
    [Description("Add a new meta message to the current channel or a specified channel.")]
    public async Task AddMessage(
        SlashCommandContext ctx,
        [Description("Optional channel to add the message to. Defaults to the current channel.")]
        DiscordChannel? channel = null)
    {
        DiscordChannel destinationChannel = channel ?? ctx.Channel;

        await _commandQueue.Writer.WriteAsync(new AddMetaMessageCommand(ctx, destinationChannel));
    }

    [Command("edit")]
    [Description("Edit meta message")]
    public async Task EditMessage(SlashCommandContext ctx, DiscordMessage message)
    {
        await _commandQueue.Writer.WriteAsync(new EditMetaMessageCommand(ctx, message));
    }
}

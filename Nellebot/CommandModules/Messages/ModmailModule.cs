﻿using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Workers;
using BaseContext = Nellebot.CommandHandlers.BaseContext;

namespace Nellebot.CommandModules.Messages;

public class ModmailModule : ApplicationCommandModule
{
    private readonly CommandParallelQueueChannel _channel;

    public ModmailModule(CommandParallelQueueChannel channel)
    {
        _channel = channel;
    }

    [SlashCommand("modmail", "Send a message via the modmail")]
    public async Task RequestModmailTicket(InteractionContext ctx)
    {
        var messageContent = "I'll just slip into your DMs";

        DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(messageContent)
            .AsEphemeral();

        await ctx.CreateResponseAsync(responseBuilder);

        var command = new RequestModmailTicketCommand(BaseContext.FromInteractionContext(ctx));

        await _channel.Writer.WriteAsync(command);
    }
}

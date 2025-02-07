using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Messages;

public class ModmailModule
{
    private readonly CommandParallelQueueChannel _channel;

    public ModmailModule(CommandParallelQueueChannel channel)
    {
        _channel = channel;
    }

    [BaseCommandCheck]
    [Command("modmail")]
    [Description("Initiate a private conversation with the moderators (will continue in DMs)")]
    public async Task RequestModmailTicket(CommandContext ctx)
    {
        const string messageContent = "I'll just slip into your DMs";

        DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(messageContent)
            .AsEphemeral();

        await ctx.RespondAsync(responseBuilder);

        var command = new RequestModmailTicketCommand(ctx);

        await _channel.Writer.WriteAsync(command);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.MessageTemplates;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Extensions;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[RequirePermissions(DiscordPermissions.None, UserPermissions = DiscordPermissions.Administrator)]
[Command("admin")]
public class AdminModule
{
    private readonly CommandQueueChannel _commandQueue;

    public AdminModule(CommandQueueChannel commandQueue)
    {
        _commandQueue = commandQueue;
    }

    [Command("nickname")]
    public async Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
    {
        ctx.Guild.ThrowIfNull();

        name = name.RemoveQuotes();

        await ctx.Guild.CurrentMember.ModifyAsync(x => x.Nickname = name);

        // TODO implement a more general way to respond to commands
        if (ctx is SlashCommandContext slashCtx)
        {
            await slashCtx.RespondAsync("Nickname changed", true);
        }
    }

    [Command("set-greeting-message")]
    public Task SetGreetingMessage(CommandContext ctx, [RemainingText] string message)
    {
        return _commandQueue.Writer.WriteAsync(new SetGreetingMessageCommand(ctx, message)).AsTask();
    }

    [Command("populate-messages")]
    public Task PopulateMessages(CommandContext ctx)
    {
        return _commandQueue.Writer.WriteAsync(new PopulateMessagesCommand(ctx)).AsTask();
    }

    [Command("delete-spam-after")]
    public async Task DeleteSpam(CommandContext ctx, ulong channelId, ulong messageId)
    {
        ctx.Guild.ThrowIfNull();

        DiscordChannel channel = await ctx.Guild.GetChannelAsync(channelId);

        var messagesToDelete = new List<DiscordMessage>();
        await foreach (DiscordMessage m in channel.GetMessagesAfterAsync(messageId, 1000))
        {
            messagesToDelete.Add(m);
        }

        await channel.DeleteMessagesAsync(messagesToDelete);

        await ctx.RespondAsync($"Deleted {messagesToDelete.Count} messages");
    }

    [Command("rebuild-ordbok")]
    public Task RebuildOrdbok(CommandContext ctx)
    {
        return _commandQueue.Writer.WriteAsync(new RebuildArticleStoreCommand(ctx)).AsTask();
    }

    [Command("run-job")]
    public Task RunJob(CommandContext ctx, string jobName)
    {
        return _commandQueue.Writer.WriteAsync(new RunJobCommand(ctx, jobName)).AsTask();
    }

    [Command("dry-run-job")]
    public Task DryRunJob(CommandContext ctx, string jobName)
    {
        return _commandQueue.Writer.WriteAsync(new RunJobCommand(ctx, jobName, true)).AsTask();
    }

    [Command("cancel-job")]
    public Task CancelJob(CommandContext ctx, string jobName)
    {
        return _commandQueue.Writer.WriteAsync(new CancelJobCommand(ctx, jobName)).AsTask();
    }
}

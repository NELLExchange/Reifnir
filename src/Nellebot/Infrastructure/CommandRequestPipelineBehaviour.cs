using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Infrastructure;

public class CommandRequestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> _logger;

    public CommandRequestPipelineBehaviour(
        ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> logger,
        IDiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (InteractionException ex) when (request is BotCommandCommand commandCommand)
        {
            await HandleInteractionException(commandCommand, ex);
            return default!;
        }
        catch (Exception ex) when (request is BotSlashCommand slashCommand)
        {
            await HandleSlashCommandException(slashCommand, ex);
            return default!;
        }
        catch (Exception ex) when (request is BotCommandCommand commandCommand)
        {
            await HandleCommandCommandException(commandCommand, ex);
            return default!;
        }
        catch (Exception ex) when (request is IRequest)
        {
            HandleRequestException(ex);
            return default!;
        }
    }

    private void HandleRequestException(Exception ex)
    {
        _discordErrorLogger.LogError(ex.Message);

        _logger.LogError(ex, nameof(HandleRequestException));
    }

    private async Task HandleInteractionException(BotCommandCommand command, InteractionException ex)
    {
        if (ex.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
        {
            DiscordInteractionResponseBuilder interactionResponse =
                new DiscordInteractionResponseBuilder().WithContent(ex.Message).AsEphemeral();
            await ex.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                interactionResponse);
        }
        else
        {
            DiscordFollowupMessageBuilder followupResponse =
                new DiscordFollowupMessageBuilder().WithContent(ex.Message).AsEphemeral();
            await ex.Interaction.CreateFollowupMessageAsync(followupResponse);
        }

        _discordErrorLogger.LogCommandError(command.Ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandleInteractionException));
    }

    private async Task HandleSlashCommandException(BotSlashCommand request, Exception ex)
    {
        SlashCommandContext ctx = request.Ctx;

        if (ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
        {
            await ctx.RespondAsync(ex.Message, ephemeral: true);
        }
        else
        {
            await ctx.FollowupAsync(ex.Message, ephemeral: true);
        }

        _discordErrorLogger.LogCommandError(ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandleSlashCommandException));
    }

    private async Task HandleCommandCommandException(BotCommandCommand request, Exception ex)
    {
        CommandContext ctx = request.Ctx;

        if (ctx is SlashCommandContext slashCtx)
        {
            if (slashCtx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            {
                await slashCtx.RespondAsync(ex.Message);
            }
            else
            {
                await slashCtx.FollowupAsync(ex.Message, ephemeral: true);
            }
        }
        else
        {
            await ctx.RespondAsync(ex.Message);
        }

        _discordErrorLogger.LogCommandError(ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandleCommandCommandException));
    }
}

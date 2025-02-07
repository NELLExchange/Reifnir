using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;
using Nellebot.Services.Loggers;

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
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex) when (request is BotCommandCommand commandCommand)
        {
            await HandeCommandCommandException(commandCommand, ex);
            return default!;
        }
        catch (Exception ex) when (request is IRequest command)
        {
            HandleRequestException(command, ex);
            return default!;
        }
    }

    private void HandleRequestException(IRequest request, Exception ex)
    {
        _discordErrorLogger.LogError(ex.Message);

        _logger.LogError(ex, nameof(HandleRequestException));
    }

    private async Task HandeCommandCommandException(BotCommandCommand request, Exception ex)
    {
        // TODO respond with ephemeral message if possible
        CommandContext ctx = request.Ctx;

        await ctx.RespondAsync(ex.Message);

        _discordErrorLogger.LogCommandError(ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandeCommandCommandException));
    }
}

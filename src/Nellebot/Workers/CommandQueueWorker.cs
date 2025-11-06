using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;

namespace Nellebot.Workers;

public class CommandQueueWorker : BackgroundService
{
    private readonly CommandQueueChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandQueueWorker> _logger;

    public CommandQueueWorker(
        ILogger<CommandQueueWorker> logger,
        CommandQueueChannel channel,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _channel = channel;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (ICommand command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (command == null)
                    continue;

                _logger.LogDebug("Dequeued command. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.Send(command, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(CommandQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}

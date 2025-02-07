using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;

namespace Nellebot.Workers;

public class CommandParallelQueueWorker : BackgroundService
{
    private readonly CommandParallelQueueChannel _channel;
    private readonly ILogger<CommandParallelQueueWorker> _logger;
    private readonly IMediator _mediator;

    public CommandParallelQueueWorker(
        ILogger<CommandParallelQueueWorker> logger,
        CommandParallelQueueChannel channel,
        IMediator mediator)
    {
        _logger = logger;
        _channel = channel;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (ICommand? command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                if (command != null)
                {
                    _logger.LogDebug(
                        "Dequeued parallel command. {RemainingMessageCount} left in queue",
                        _channel.Reader.Count);

                    _ = Task.Run(() => _mediator.Send(command, stoppingToken), stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(CommandParallelQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}

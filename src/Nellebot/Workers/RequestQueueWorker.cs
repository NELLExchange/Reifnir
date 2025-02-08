using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nellebot.Workers;

public class RequestQueueWorker : BackgroundService
{
    private readonly RequestQueueChannel _channel;
    private readonly ILogger<RequestQueueWorker> _logger;
    private readonly IMediator _mediator;

    public RequestQueueWorker(ILogger<RequestQueueWorker> logger, RequestQueueChannel channel, IMediator mediator)
    {
        _logger = logger;
        _channel = channel;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (IRequest? command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                if (command != null)
                {
                    _logger.LogDebug("Dequeued request. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    _ = Task.Run(() => _mediator.Send(command, stoppingToken), stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(RequestQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}

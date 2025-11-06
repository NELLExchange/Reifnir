using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.Infrastructure;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;

namespace Nellebot.Workers;

public class EventQueueWorker : BackgroundService
{
    private readonly EventQueueChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<EventQueueWorker> _logger;

    public EventQueueWorker(
        ILogger<EventQueueWorker> logger,
        EventQueueChannel channel,
        IServiceScopeFactory scopeFactory,
        IDiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _channel = channel;
        _scopeFactory = scopeFactory;
        _discordErrorLogger = discordErrorLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (INotification notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _logger.LogDebug("Dequeued event. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var publisher = scope.ServiceProvider.GetRequiredService<NotificationPublisher>();

                    await publisher.Publish(notification, stoppingToken);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerEx in ex.InnerExceptions)
                    {
                        if (notification is EventNotification eventNotification)
                        {
                            // TODO extract relevant information from notification object
                            // and pass to LogEventError
                            _discordErrorLogger.LogError(innerEx, nameof(EventQueueWorker));
                        }

                        _logger.LogError(innerEx, nameof(EventQueueWorker));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, nameof(EventQueueWorker));
                    _discordErrorLogger.LogError(ex.Message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(EventQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(EventQueueWorker));
            _discordErrorLogger.LogError(ex.Message);
        }
    }
}

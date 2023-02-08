﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.NotificationHandlers;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.Workers;

public class EventQueueWorker : BackgroundService
{
    private readonly ILogger<EventQueueWorker> _logger;
    private readonly EventQueueChannel _channel;
    private readonly NotificationPublisher _publisher;
    private readonly IDiscordErrorLogger _discordErrorLogger;

    public EventQueueWorker(ILogger<EventQueueWorker> logger, EventQueueChannel channel, NotificationPublisher publisher, IDiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _channel = channel;
        _publisher = publisher;
        _discordErrorLogger = discordErrorLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        INotification? @event = null;

        try
        {
            await foreach (INotification notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                @event = notification;

                if (@event != null)
                {
                    _logger.LogDebug("Dequeued event. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    await _publisher.Publish(@event, stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(EventQueueWorker));
        }
        catch (AggregateException ex)
        {
            foreach (Exception innerEx in ex.InnerExceptions)
            {
                if (@event is not null and EventNotification notification)
                {
                    // TODO remove await when error logger methods become synchronous
                    if (notification.Ctx != null)
                        _discordErrorLogger.LogEventError(notification.Ctx, innerEx.ToString());
                    else
                        _discordErrorLogger.LogError(innerEx, nameof(EventQueueWorker));
                }

                _logger.LogError(innerEx, nameof(EventQueueWorker));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(EventQueueWorker));
        }
    }
}

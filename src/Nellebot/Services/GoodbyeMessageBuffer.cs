using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nellebot.Infrastructure;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Services;

public class GoodbyeMessageBuffer
{
    private const int DelayInMs = 5000;

    private readonly BatchingBuffer<string> _buffer;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly NotificationPublisher _notificationPublisher;

    public GoodbyeMessageBuffer(IDiscordErrorLogger discordErrorLogger, NotificationPublisher notificationPublisher)
    {
        _buffer = new BatchingBuffer<string>(DelayInMs, PublishBufferedEvent);
        _discordErrorLogger = discordErrorLogger;
        _notificationPublisher = notificationPublisher;
    }

    public void AddUser(string user)
    {
        _buffer.AddMessage(user);
    }

    private async Task PublishBufferedEvent(IEnumerable<string> users)
    {
        try
        {
            await _notificationPublisher.Publish(new BufferedMemberLeftNotification(users), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, "Error publishing buffered event");
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Common.Extensions;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.NotificationHandlers;

public class ClientStatusHandler : INotificationHandler<ClientHeartbeatNotification>,
    INotificationHandler<SessionCreatedNotification>,
    INotificationHandler<SessionResumedOrDownloadCompletedNotification>,
    INotificationHandler<ClientConnected>,
    INotificationHandler<ClientDisconnected>
{
    // POC: Make better
    private static bool IsClientActuallyReady;

    private readonly BotSettingsService _botSettingsService;
    private readonly DiscordLogger _discordLogger;
    private readonly ILogger<ClientStatusHandler> _logger;
    private readonly DiscordClient _client;
    private readonly MessageRefsService _messageRefsService;
    private readonly BotOptions _options;

    public ClientStatusHandler(
        BotSettingsService botSettingsService,
        MessageRefsService messageRefsService,
        DiscordLogger discordLogger,
        ILogger<ClientStatusHandler> logger,
        IOptions<BotOptions> options,
        DiscordClient client)
    {
        _botSettingsService = botSettingsService;
        _messageRefsService = messageRefsService;
        _discordLogger = discordLogger;
        _logger = logger;
        _client = client;
        _options = options.Value;
    }

    public async Task Handle(ClientHeartbeatNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogTrace(
            "Heartbeat: {heartbeat}, Ping: {ping}ms",
            notification.Timestamp.ToIsoDateTimeString(),
            notification.Ping.TotalMilliseconds);

        if (!IsClientActuallyReady)
        {
            _logger.LogDebug("Client not actually ready. Skipping heartbeat save.");
            return;
        }

        await _botSettingsService.SetLastHeartbeat(notification.Timestamp);
    }

    public Task Handle(SessionCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Session Created");
        return Task.CompletedTask;
    }

    public async Task Handle(
        SessionResumedOrDownloadCompletedNotification notification,
        CancellationToken cancellationToken)
    {
        DateTimeOffset lastHeartbeat = await _botSettingsService.GetLastHeartbeat() ?? DateTimeOffset.UtcNow;

        TimeSpan timeSinceLastHeartbeat = DateTimeOffset.UtcNow - lastHeartbeat;

        if (timeSinceLastHeartbeat.TotalMinutes > 1)
        {
            var message = $"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}.";
            message += $" More than {timeSinceLastHeartbeat.TotalMinutes:0.00} minutes since last heartbeat.";

            _logger.LogDebug("{message}", message);

            _discordLogger.LogExtendedActivityMessage(message);
        }

        if (_options.AutoPopulateMessagesOnReadyEnabled)
        {
            int createdCount = await _messageRefsService.PopulateMessageRefs(lastHeartbeat, notification.EventSource);

            if (createdCount > 0)
            {
                _logger.LogDebug("Populated {createdCount} message refs", createdCount);
                _discordLogger.LogExtendedActivityMessage($"Populated {createdCount} message refs");
            }
        }

        IsClientActuallyReady = true;
    }

    public Task Handle(ClientConnected notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot connected");
        return Task.CompletedTask;
    }

    public Task Handle(ClientDisconnected notification, CancellationToken cancellationToken)
    {
        IsClientActuallyReady = false;

        _logger.LogInformation("Bot disconnected {message}", notification.EventArgs.CloseMessage);

        return Task.CompletedTask;
    }
}

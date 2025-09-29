using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Workers;

namespace Nellebot.Services.Loggers;

public class DiscordLogger
{
    private readonly DiscordLogChannel _channel;
    private readonly ILogger<DiscordLogger> _logger;
    private readonly BotOptions _options;

    public DiscordLogger(IOptions<BotOptions> options, DiscordLogChannel channel, ILogger<DiscordLogger> logger)
    {
        _options = options.Value;
        _channel = channel;
        _logger = logger;
    }

    public void LogGreetingMessage(string message)
    {
        LogMessageCore(message, _options.GreetingsChannelId);
    }

    public void LogQuarantineMessage(string message)
    {
        LogMessageCore(message, _options.QuarantineChannelId);
    }

    public void LogActivityMessage(string message)
    {
        LogMessageCore(message, _options.ActivityLogChannelId, suppressNotifications: true);
    }

    public void LogActivityMessage(DiscordMessageBuilder message)
    {
        LogMessageCore(message, _options.ActivityLogChannelId, suppressNotifications: true);
    }

    public void LogOperationMessage(string message)
    {
        LogMessageCore(message, _options.OperationLogChannelId, suppressNotifications: true);
    }

    public void LogModAlertsMessage(string message)
    {
        LogMessageCore(message, _options.ModAlertsChannelId);
    }

    private void LogMessageCore(string message, ulong channelId, bool suppressNotifications = false)
    {
        ulong guildId = _options.GuildId;

        var discordLogItem = new DiscordLogItem<string>(message, guildId, channelId, suppressNotifications);

        if (!_channel.Writer.TryWrite(discordLogItem))
            _logger.LogError("Could not write to DiscordLogChannel. Message: {message}", message);
    }

    private void LogMessageCore(DiscordMessageBuilder message, ulong channelId, bool suppressNotifications = false)
    {
        ulong guildId = _options.GuildId;

        var discordLogItem = new DiscordLogItem<DiscordMessageBuilder>(
            message,
            guildId,
            channelId,
            suppressNotifications);

        if (!_channel.Writer.TryWrite(discordLogItem))
            _logger.LogError("Could not write to DiscordLogChannel. Message: {message}", message);
    }
}

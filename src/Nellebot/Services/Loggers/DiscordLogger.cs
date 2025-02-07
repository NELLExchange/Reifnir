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

    public void LogActivityMessage(string message)
    {
        LogMessageCore(message, _options.ActivityLogChannelId);
    }

    public void LogExtendedActivityMessage(string message)
    {
        LogMessageCore(message, _options.ExtendedActivityLogChannelId);
    }

    public void LogTrustedChannelMessage(string message)
    {
        LogMessageCore(message, _options.TrustedChannelId);
    }

    private void LogMessageCore(string message, ulong channelId)
    {
        ulong guildId = _options.GuildId;

        var discordLogItem = new DiscordLogItem<string>(message, guildId, channelId);

        bool sucess = _channel.Writer.TryWrite(discordLogItem);

        if (!sucess) _logger.LogError("Could not write to DiscordLogChannel");
    }
}

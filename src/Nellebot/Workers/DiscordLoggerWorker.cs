using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nellebot.Workers;

public class DiscordLoggerWorker : BackgroundService
{
    private readonly DiscordLogChannel _channel;
    private readonly DiscordClient _client;
    private readonly ILogger<DiscordLoggerWorker> _logger;
    private readonly BotOptions _options;

    public DiscordLoggerWorker(
        ILogger<DiscordLoggerWorker> logger,
        DiscordLogChannel channel,
        DiscordClient client,
        IOptions<BotOptions> options)
    {
        _logger = logger;
        _channel = channel;
        _client = client;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (BaseDiscordLogItem logItem in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                DiscordChannel logChannel = await ResolverLogChannel(logItem.DiscordGuildId, logItem.DiscordChannelId);

                try
                {
                    switch (logItem)
                    {
                        case DiscordLogItem<string> discordLogItem:
                            await logChannel.SendMessageAsync(discordLogItem.Message);
                            break;
                        case DiscordLogItem<DiscordEmbed> discordEmbedLogItem:
                            await logChannel.SendMessageAsync(discordEmbedLogItem.Message);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (Exception ex)
                {
                    await LogLoggingFailure(ex, logItem.DiscordGuildId);
                }

                _logger.LogDebug(
                    "Dequeued (parallel) command. {RemainingMessageCount} left in queue",
                    _channel.Reader.Count);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(DiscordLoggerWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }

    private async Task LogLoggingFailure(Exception exception, ulong discordGuildId)
    {
        try
        {
            _logger.LogError(exception, "{Error}", exception.Message);

            DiscordChannel errorLogChannel = await ResolverLogChannel(discordGuildId, _options.ErrorLogChannelId);

            ArgumentNullException.ThrowIfNull(errorLogChannel);

            await errorLogChannel.SendMessageAsync(
                $"Failed to log original error message. Reason: {exception.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Welp");
        }
    }

    // There is a DiscordResolver method for resolving channels...
    // but that class depends on DiscordLogger so it's just simpler
    // to have a duplicate method than dealing with circular dependency
    private async Task<DiscordChannel> ResolverLogChannel(ulong guildId, ulong channelId)
    {
        _client.Guilds.TryGetValue(guildId, out DiscordGuild? discordGuild);

        discordGuild ??= await _client.GetGuildAsync(guildId);

        discordGuild.Channels.TryGetValue(channelId, out DiscordChannel? discordChannel);

        return discordChannel ??= await discordGuild.GetChannelAsync(channelId);
    }
}

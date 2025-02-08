using System;
using System.Threading.Tasks;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;

namespace Nellebot.Services;

public class BotSettingsService
{
    private static readonly string GreetingMessageKey = "GreetingMessage";
    private static readonly string GreetingMessageUserVariable = "$USER";
    private static readonly string LastHeartbeatKey = "LastHeartbeat";

    private readonly BotSettingsRepository _botSettingsRepo;
    private readonly SharedCache _cache;

    public BotSettingsService(BotSettingsRepository botSettingsRepos, SharedCache cache)
    {
        _botSettingsRepo = botSettingsRepos;
        _cache = cache;
    }

    public async Task SetGreetingMessage(string message)
    {
        await _botSettingsRepo.SaveBotSetting(GreetingMessageKey, message);

        _cache.FlushCache(SharedCacheKeys.GreetingMessage);
    }

    public async Task<string?> GetGreetingsMessage(string userMention)
    {
        string? messageTemplate = await _cache.LoadFromCacheAsync(
            SharedCacheKeys.GreetingMessage,
            () => _botSettingsRepo.GetBotSetting(
                SharedCacheKeys
                    .GreetingMessage),
            TimeSpan.FromMinutes(5));

        if (messageTemplate == null) return null;

        string message = messageTemplate.Replace(GreetingMessageUserVariable, userMention);

        return message;
    }

    public Task SetLastHeartbeat(DateTimeOffset heartbeatDateTime)
    {
        return _botSettingsRepo.SaveBotSetting(LastHeartbeatKey, heartbeatDateTime.ToUnixTimeMilliseconds().ToString());
    }

    public async Task<DateTimeOffset?> GetLastHeartbeat()
    {
        string? lastHeartBeatStringValue = await _botSettingsRepo.GetBotSetting(LastHeartbeatKey);

        if (lastHeartBeatStringValue == null) return null;

        bool parsed = long.TryParse(lastHeartBeatStringValue, out long lastHeartBeatTicks);

        if (!parsed) return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(lastHeartBeatTicks);
    }
}

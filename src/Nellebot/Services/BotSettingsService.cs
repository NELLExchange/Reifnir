using System;
using System.Threading.Tasks;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;

namespace Nellebot.Services;

public class BotSettingsService
{
    private const string GreetingMessageKey = "GreetingMessage";
    private const string QuarantineMessageKey = "QuarantineMessage";
    private const string MessageTemplateUserVariable = "$USER";
    private const string MessageTemplateReasonVariable = "$REASON";
    private const string LastHeartbeatKey = "LastHeartbeat";

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

    public async Task<string?> GetGreetingMessage(string userMention)
    {
        string? messageTemplate = await _cache.LoadFromCacheAsync(
            SharedCacheKeys.GreetingMessage,
            () => _botSettingsRepo.GetBotSetting(
                SharedCacheKeys
                    .GreetingMessage),
            TimeSpan.FromMinutes(5));

        string? message = messageTemplate?.Replace(MessageTemplateUserVariable, userMention);

        return message;
    }

    public async Task SetQuarantineMessage(string message)
    {
        await _botSettingsRepo.SaveBotSetting(QuarantineMessageKey, message);

        _cache.FlushCache(SharedCacheKeys.QuarantineMessage);
    }

    public async Task<string?> GetQuarantineMessage(string userMention, string reason)
    {
        string? messageTemplate = await _cache.LoadFromCacheAsync(
            SharedCacheKeys.QuarantineMessage,
            () => _botSettingsRepo.GetBotSetting(
                SharedCacheKeys
                    .QuarantineMessage),
            TimeSpan.FromMinutes(5));

        if (messageTemplate == null) return null;

        string message = messageTemplate.Replace(MessageTemplateUserVariable, userMention);
        message = message.Replace(MessageTemplateReasonVariable, reason);

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

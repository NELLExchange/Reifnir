using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Quartz;

namespace Nellebot.Jobs;

public class InsideGoodbyeJob : IJob
{
    public static readonly JobKey Key = new("inside-job", "default");

    private const string GoodbyeMessageTemplateType = "goodbye";
    private const string FallbackGoodbyeMessageTemplate = "$USER has left. Goodbye!";
    private const int MessageTemplatesCacheDurationMinutes = 5;
    private const int MaxMemberResolutionAttempts = 5;

    private static readonly DateTimeOffset WindowStart = new(2026, 4, 1, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 4, 1, 22, 0, 0, TimeSpan.Zero);

    private readonly BotOptions _options;
    private readonly MessageRefRepository _messageRefRepo;
    private readonly MessageTemplateRepository _messageTemplateRepo;
    private readonly DiscordResolver _discordResolver;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly SharedCache _cache;

    public InsideGoodbyeJob(
        IOptions<BotOptions> options,
        MessageRefRepository messageRefRepo,
        MessageTemplateRepository messageTemplateRepo,
        DiscordResolver discordResolver,
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger,
        SharedCache cache)
    {
        _options = options.Value;
        _messageRefRepo = messageRefRepo;
        _messageTemplateRepo = messageTemplateRepo;
        _discordResolver = discordResolver;
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _cache = cache;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _discordLogger.LogOperationMessage($"Job started: {Key}");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (now < WindowStart || now >= WindowEnd)
            {
                _discordLogger.LogOperationMessage($"Hold your horses: {Key}");
                return;
            }

            DateTime userSelectionCutOff = now.UtcDateTime - TimeSpan.FromHours(4);

            List<ulong> userIds = await _messageRefRepo.GetDistinctUserIdsByChannelAfterDateTime(
                _options.GreetingsChannelId,
                userSelectionCutOff);

            if (userIds.Count == 0)
                return;

            var rng = new Random();
            List<ulong> shuffled = userIds.OrderBy(_ => rng.Next()).ToList();

            DiscordMember? member = null;
            int attempts = Math.Min(MaxMemberResolutionAttempts, shuffled.Count);

            for (int i = 0; i < attempts; i++)
            {
                member = await _discordResolver.ResolveGuildMember(shuffled[i]);
                if (member is not null) break;
            }

            if (member is null)
                return;

            string message = await GetRandomGoodbyeMessage(member.DisplayName);

            _discordLogger.LogGreetingMessage(message);

            _discordLogger.LogOperationMessage($"Job finished: {Key}");
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, ex.Message);
            throw new JobExecutionException(ex);
        }
    }

    private async Task<string> GetRandomGoodbyeMessage(string username)
    {
        List<MessageTemplate> goodbyeMessages = (await _cache.LoadFromCacheAsync(
                                                     SharedCacheKeys.GoodbyeMessages,
                                                     async () =>
                                                         await _messageTemplateRepo
                                                             .GetAllMessageTemplates(GoodbyeMessageTemplateType),
                                                     TimeSpan.FromMinutes(MessageTemplatesCacheDurationMinutes))
                                                 ?? [])
            .ToList();

        string messageTemplate;

        if (goodbyeMessages.Count > 0)
        {
            int idx = new Random().Next(minValue: 0, goodbyeMessages.Count);
            messageTemplate = goodbyeMessages[idx].Message;
        }
        else
        {
            messageTemplate = FallbackGoodbyeMessageTemplate;
        }

        return messageTemplate.Replace("$USER", username);
    }
}

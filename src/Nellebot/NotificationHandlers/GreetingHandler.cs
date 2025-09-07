using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.NotificationHandlers;

public class GreetingHandler :
    INotificationHandler<GuildMemberRemovedNotification>,
    INotificationHandler<BufferedMemberLeftNotification>,
    INotificationHandler<MemberApprovedNotification>,
    INotificationHandler<MemberQuarantinedNotification>
{
    private const int MaxUsernamesToDisplay = 100;
    private const string FallbackGreetingMessage = "Welcome, $USER!";
    private const string FallbackQuarantineMessage = "Welcome to quarantine, $USER!";
    private const string GoodbyeMessageTemplateType = "goodbye";
    private const string FallbackGoodbyeMessageTemplate = "$USER has left. Goodbye!";
    private const int MessageTemplatesCacheDurationMinutes = 5;

    private readonly BotSettingsService _botSettingsService;
    private readonly SharedCache _cache;
    private readonly BotOptions _botOptions;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly GoodbyeMessageBuffer _goodbyeMessageBuffer;
    private readonly MessageTemplateRepository _messageTemplateRepo;

    public GreetingHandler(
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger,
        BotSettingsService botSettingsService,
        GoodbyeMessageBuffer goodbyeMessageBuffer,
        MessageTemplateRepository messageTemplateRepo,
        SharedCache cache,
        IOptions<BotOptions> botOptions)
    {
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _botSettingsService = botSettingsService;
        _goodbyeMessageBuffer = goodbyeMessageBuffer;
        _messageTemplateRepo = messageTemplateRepo;
        _cache = cache;
        _botOptions = botOptions.Value;
    }

    public async Task Handle(BufferedMemberLeftNotification notification, CancellationToken cancellationToken)
    {
        List<string> userList = notification.Usernames.ToList();

        if (userList.Count == 0) return;

        string? greetingMessage;

        switch (userList.Count)
        {
            case 1:
                greetingMessage = await GetRandomGoodbyeMessage(userList.First());
                break;

            case <= MaxUsernamesToDisplay:
            {
                string userListOutput = string.Join(", ", userList.Select(x => $"**{x}**"));
                greetingMessage = $"The following users have left the server: {userListOutput}. Goodbye!";
                break;
            }

            default:
            {
                IEnumerable<string> usersToShow = userList.Take(MaxUsernamesToDisplay);
                int remainingCount = userList.Count - MaxUsernamesToDisplay;
                string usersToShowOutput = string.Join(", ", usersToShow.Select(x => $"**{x}**"));

                greetingMessage =
                    $"The following users have left the server: {usersToShowOutput} and {remainingCount} others. Goodbye!";
                break;
            }
        }

        _discordLogger.LogGreetingMessage(greetingMessage);
    }

    public async Task Handle(MemberApprovedNotification notification, CancellationToken cancellationToken)
    {
        DiscordMember newMember = notification.Member;
        string memberMention = newMember.Mention;

        string? greetingMessage = await _botSettingsService.GetGreetingMessage(memberMention);

        if (greetingMessage == null)
        {
            greetingMessage = FallbackGreetingMessage;

            _discordErrorLogger.LogError("Greeting message couldn't be retrieved");
        }

        _discordLogger.LogGreetingMessage(greetingMessage);
    }

    public async Task Handle(MemberQuarantinedNotification notification, CancellationToken cancellationToken)
    {
        DiscordMember newMember = notification.Member;
        string memberMention = newMember.Mention;

        string? quarantineMessage = await _botSettingsService.GetQuarantineMessage(memberMention);

        if (quarantineMessage == null)
        {
            quarantineMessage = FallbackQuarantineMessage;

            _discordErrorLogger.LogError("Quarantine message couldn't be retrieved");
        }

        _discordLogger.LogQuarantineMessage(quarantineMessage);
    }

    public Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
    {
        DiscordMember member = notification.EventArgs.Member;
        string memberName = member.DisplayName;

        TimeSpan memberJoinedAgo = DateTimeOffset.UtcNow - member.JoinedAt;
        bool memberIsQuarantined = member.Roles.Any(r => r.Id == _botOptions.QuarantineRoleId);

        // Don't want to say goodbye to quarantined users who just recently joined,
        // since they were most likely not greeted either.
        const int minQuarantineJoinDateForGoodbyeDays = 2;
        bool skipSayingGoodbye = memberIsQuarantined
                                 && memberJoinedAgo < TimeSpan.FromDays(minQuarantineJoinDateForGoodbyeDays);

        if (!skipSayingGoodbye)
            _goodbyeMessageBuffer.AddUser(memberName);

        return Task.CompletedTask;
    }

    private async Task<string> GetRandomGoodbyeMessage(string username)
    {
        string messageTemplate;

        List<MessageTemplate> goodbyeMessages = (await _cache.LoadFromCacheAsync(
                                                     SharedCacheKeys.GoodbyeMessages,
                                                     async () =>
                                                         await _messageTemplateRepo
                                                             .GetAllMessageTemplates(GoodbyeMessageTemplateType),
                                                     TimeSpan
                                                         .FromMinutes(MessageTemplatesCacheDurationMinutes))
                                                 ?? Enumerable.Empty<MessageTemplate>())
            .ToList();

        if (goodbyeMessages.Count > 0)
        {
            int idx = new Random().Next(minValue: 0, goodbyeMessages.Count);
            messageTemplate = goodbyeMessages[idx].Message;
        }
        else
        {
            messageTemplate = FallbackGoodbyeMessageTemplate;
        }

        string formattedGoodbyeMessage = messageTemplate.Replace("$USER", username);

        return formattedGoodbyeMessage;
    }
}

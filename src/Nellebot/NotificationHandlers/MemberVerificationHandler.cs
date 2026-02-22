using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.NotificationHandlers;

public class MemberVerificationHandler : INotificationHandler<GuildMemberAddedNotification>
{
    private const int ApproveDelayDurationSeconds = 10;

    private readonly DiscordResolver _discordResolver;
    private readonly EventQueueChannel _eventQueueChannel;
    private readonly QuarantineService _quarantineService;
    private readonly BotOptions _botOptions;

    public MemberVerificationHandler(
        DiscordResolver discordResolver,
        EventQueueChannel eventQueueChannel,
        QuarantineService quarantineService,
        IOptions<BotOptions> botOptions)
    {
        _discordResolver = discordResolver;
        _eventQueueChannel = eventQueueChannel;
        _quarantineService = quarantineService;
        _botOptions = botOptions.Value;
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberAddedEventArgs args = notification.EventArgs;
        DiscordMember member = args.Member;

        // Check if user needs to be quarantined for having a brand-new account
        TimeSpan memberAccountAge = DateTimeOffset.UtcNow - member.Id.GetSnowflakeTime();

        DiscordMember botMember = _discordResolver.GetBotMember();

        var suspiciousAccountAgeThresholdDays = _botOptions.SuspiciousAccountAgeThresholdDays;

        if (memberAccountAge < TimeSpan.FromDays(suspiciousAccountAgeThresholdDays))
        {
            var quarantineReason = $"User account is less than {suspiciousAccountAgeThresholdDays} days old.";
            await _quarantineService.QuarantineMember(member, botMember, quarantineReason);
        }
        else
        {
            // Wait for any post-join quarantine actions
            await Task.Delay(TimeSpan.FromSeconds(ApproveDelayDurationSeconds), cancellationToken);

            DiscordMember? updatedMember = await _discordResolver.ResolveGuildMember(member.Id);

            // If the member is null for some reason, approve them anyway
            if (updatedMember is not null)
            {
                bool memberIsQuarantined = member.Roles.Any(r => r.Id == _botOptions.QuarantineRoleId);

                if (memberIsQuarantined)
                {
                    // No greet for you!
                    return;
                }
            }

            await _eventQueueChannel.Writer.WriteAsync(
                new MemberApprovedNotification(member, botMember),
                cancellationToken);
        }
    }
}

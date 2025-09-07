using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Nellebot.Services;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.NotificationHandlers;

public class MemberVerificationHandler : INotificationHandler<GuildMemberAddedNotification>
{
    private readonly DiscordResolver _discordResolver;
    private readonly EventQueueChannel _eventQueueChannel;
    private readonly QuarantineService _quarantineService;

    public MemberVerificationHandler(
        DiscordResolver discordResolver,
        EventQueueChannel eventQueueChannel,
        QuarantineService quarantineService)
    {
        _discordResolver = discordResolver;
        _eventQueueChannel = eventQueueChannel;
        _quarantineService = quarantineService;
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberAddedEventArgs args = notification.EventArgs;
        DiscordMember member = args.Member;

        // Check if user needs to be quarantined for having a brand-new account
        const int suspiciousAccountAgeThresholdDays = 2;
        TimeSpan memberAccountAge = DateTimeOffset.UtcNow - member.Id.GetSnowflakeTime();

        DiscordMember botMember = _discordResolver.GetBotMember();

        if (memberAccountAge < TimeSpan.FromDays(suspiciousAccountAgeThresholdDays))
        {
            var quarantineReason = $"User account age under threshold (< {suspiciousAccountAgeThresholdDays} days)";
            await _quarantineService.QuarantineMember(member, botMember, quarantineReason);
        }
        else
        {
            await _eventQueueChannel.Writer.WriteAsync(
                new MemberApprovedNotification(member, botMember),
                cancellationToken);
        }
    }
}

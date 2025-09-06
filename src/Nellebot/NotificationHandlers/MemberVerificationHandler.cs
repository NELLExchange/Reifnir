using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.NotificationHandlers;

public class MemberVerificationHandler : INotificationHandler<GuildMemberAddedNotification>
{
    private readonly BotOptions _botOptions;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly EventQueueChannel _eventQueueChannel;

    public MemberVerificationHandler(
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver,
        EventQueueChannel eventQueueChannel,
        IOptions<BotOptions> botOptions)
    {
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _eventQueueChannel = eventQueueChannel;
        _botOptions = botOptions.Value;
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
            await QuarantineMember(member, botMember, quarantineReason);
        }
        else
        {
            await _eventQueueChannel.Writer.WriteAsync(
                new MemberApprovedNotification(member, botMember),
                cancellationToken);
        }
    }

    private async Task QuarantineMember(DiscordMember member, DiscordMember memberResponsible, string quarantineReason)
    {
        string memberIdentifier = member.GetDetailedMemberIdentifier();
        ulong quarantineRoleId = _botOptions.QuarantineRoleId;
        DiscordRole? quarantineRole = _discordResolver.ResolveRole(quarantineRoleId);
        if (quarantineRole is not null)
        {
            await member.GrantRoleAsync(quarantineRole, quarantineReason);

            await _eventQueueChannel.Writer.WriteAsync(
                new MemberQuarantinedNotification(member, memberResponsible, quarantineReason));
        }
        else
        {
            _discordErrorLogger.LogError(
                $"Attempted to quarantine member {memberIdentifier}, but was unable to resolve quarantine role");
        }
    }
}

using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.Services;

public class QuarantineService
{
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly EventQueueChannel _eventQueueChannel;
    private readonly BotOptions _botOptions;

    public QuarantineService(
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

    public async Task QuarantineMember(DiscordMember member, DiscordMember memberResponsible, string quarantineReason)
    {
        string memberIdentifier = member.GetDetailedMemberIdentifier();
        ulong quarantineRoleId = _botOptions.QuarantineRoleId;
        DiscordRole? quarantineRole = _discordResolver.ResolveRole(quarantineRoleId);

        if (quarantineRole is null)
        {
            _discordErrorLogger.LogError(
                $"Attempted to quarantine member {memberIdentifier}, but was unable to resolve quarantine role");
            return;
        }

        await member.GrantRoleAsync(quarantineRole, quarantineReason);

        await _eventQueueChannel.Writer.WriteAsync(
            new MemberQuarantinedNotification(member, memberResponsible, quarantineReason));
    }

    public async Task ApproveMember(DiscordMember member, DiscordMember memberResponsible)
    {
        string memberIdentifier = member.GetDetailedMemberIdentifier();
        ulong quarantineRoleId = _botOptions.QuarantineRoleId;
        DiscordRole? quarantineRole = _discordResolver.ResolveRole(quarantineRoleId);

        if (quarantineRole is null)
        {
            _discordErrorLogger.LogError(
                $"Attempted to approve member {memberIdentifier}, but was unable to resolve quarantine role");
            return;
        }

        await member.RevokeRoleAsync(quarantineRole, $"Approved by {memberResponsible.Username}");

        await _eventQueueChannel.Writer.WriteAsync(new MemberApprovedNotification(member, memberResponsible));
    }
}

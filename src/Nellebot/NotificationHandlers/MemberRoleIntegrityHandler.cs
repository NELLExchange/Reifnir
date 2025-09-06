using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Jobs;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;
using Quartz;

namespace Nellebot.NotificationHandlers;

public class MemberRoleIntegrityHandler : INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly ILogger<MemberRoleIntegrityHandler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly EventQueueChannel _eventQueueChannel;
    private readonly BotOptions _options;

    public MemberRoleIntegrityHandler(
        ILogger<MemberRoleIntegrityHandler> logger,
        IOptions<BotOptions> options,
        ISchedulerFactory schedulerFactory,
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver,
        EventQueueChannel eventQueueChannel)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _eventQueueChannel = eventQueueChannel;
        _options = options.Value;
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberUpdatedEventArgs args = notification.EventArgs;
        List<DiscordRole> addedRoles = args.RolesAfter.ExceptBy(args.RolesBefore.Select(r => r.Id), x => x.Id).ToList();
        List<DiscordRole> removedRoles =
            args.RolesBefore.ExceptBy(args.RolesAfter.Select(r => r.Id), x => x.Id).ToList();

        int roleChangesCount = addedRoles.Count + removedRoles.Count;

        if (roleChangesCount == 0) return;

        DiscordMember member = args.Member ?? throw new Exception(nameof(member));
        DiscordGuild guild = args.Guild;

        ulong[] memberRoleIds = _options.MemberRoleIds;
        ulong memberRoleId = _options.MemberRoleId;
        ulong ghostRoleId = _options.GhostRoleId;
        ulong quarantineRoleId = _options.QuarantineRoleId;

        await QuarantineIfSpammer(member, addedRoles);

        // TODO handle this in a more robust way.
        // Either pause the job, if possible, or just let it run
        // and make sure it doesn't clash with this notification handler.
        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        IReadOnlyCollection<IJobExecutionContext> jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        bool roleMaintenanceIsRunning = jobs.Any(j => Equals(j.JobDetail.Key, RoleMaintenanceJob.Key));

        if (roleMaintenanceIsRunning)
        {
            _logger.LogDebug("Role maintenance job is currently running, skipping role integrity check");
            return;
        }

        await MaintainMemberRole(guild, memberRoleId, member, memberRoleIds, quarantineRoleId);

        await MaintainGhostRole(guild, ghostRoleId, member);
    }

    /// <summary>
    /// Ensures that the member role is added if the user has any of the member roles,
    /// and removed if the user has none of the member roles
    /// </summary>
    private static async Task MaintainMemberRole(
        DiscordGuild guild,
        ulong memberRoleId,
        DiscordMember member,
        ulong[] memberRoleIds,
        ulong quarantineRoleId)
    {
        DiscordRole memberRole = guild.Roles[memberRoleId]
                                 ?? throw new Exception($"Could not find member role with id {memberRoleId}");

        bool userHasMandatoryRoles = member.Roles.Any(r => memberRoleIds.Contains(r.Id));
        bool userHasMemberRole = member.Roles.Any(r => r.Id == memberRoleId);
        bool userHasQuarantineRole = member.Roles.Any(r => r.Id == quarantineRoleId);

        bool userIsEligibleForMemberRole = userHasMandatoryRoles && !userHasQuarantineRole;

        if (!userHasMemberRole && userIsEligibleForMemberRole)
        {
            await member.GrantRoleAsync(memberRole);
        }
        else if (userHasMemberRole && !userIsEligibleForMemberRole)
        {
            await member.RevokeRoleAsync(memberRole);
        }
    }

    /// <summary>
    /// Ensures that the ghost role is added if the user has no roles,
    /// and removed if the user has any other roles
    /// </summary>
    private static async Task MaintainGhostRole(DiscordGuild guild, ulong ghostRoleId, DiscordMember member)
    {
        DiscordRole ghostRole = guild.Roles[ghostRoleId]
                                ?? throw new Exception($"Could not find ghost role with id {ghostRoleId}");

        bool userHasNoRoles = !member.Roles.Any();

        if (userHasNoRoles)
        {
            await member.GrantRoleAsync(ghostRole);
            return;
        }

        bool userHasGhostRole = member.Roles.Any(r => r.Id == ghostRoleId);
        bool userHasAnyOtherRole = member.Roles.Any(r => r.Id != ghostRoleId);

        if (userHasGhostRole && userHasAnyOtherRole)
        {
            await member.RevokeRoleAsync(ghostRole);
        }
    }

    private async Task QuarantineIfSpammer(DiscordMember member, List<DiscordRole> addedRoles)
    {
        ulong spammerRoleId = _options.SpammerRoleId;

        TimeSpan memberJoinedAgo = DateTimeOffset.UtcNow - member.JoinedAt;
        DiscordRole? addedSpammerRole = addedRoles.FirstOrDefault(r => r.Id == spammerRoleId);
        bool hasChosenSpammerRole = addedSpammerRole is not null;
        const int maxJoinAgeForAutomatedQuarantineDays = 7;

        bool shouldQuarantineSpammer = hasChosenSpammerRole
                                       && memberJoinedAgo < TimeSpan.FromDays(maxJoinAgeForAutomatedQuarantineDays);

        if (!shouldQuarantineSpammer) return;

        var quarantineReason = $"User is a **{addedSpammerRole!.Name}**";
        DiscordMember botMember = _discordResolver.GetBotMember();
        await QuarantineMember(member, botMember, quarantineReason);
    }

    private async Task QuarantineMember(DiscordMember member, DiscordMember memberResponsible, string quarantineReason)
    {
        string memberIdentifier = member.GetDetailedMemberIdentifier();
        ulong quarantineRoleId = _options.QuarantineRoleId;
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

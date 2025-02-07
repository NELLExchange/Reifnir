using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.NotificationHandlers;

public class MemberRoleIntegrityHandler : INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly ILogger<MemberRoleIntegrityHandler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly BotOptions _options;

    public MemberRoleIntegrityHandler(
        ILogger<MemberRoleIntegrityHandler> logger,
        IOptions<BotOptions> options,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _options = options.Value;
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        IReadOnlyCollection<IJobExecutionContext> jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        bool roleMaintenanceIsRunning = jobs.Any(j => Equals(j.JobDetail.Key, RoleMaintenanceJob.Key));

        if (roleMaintenanceIsRunning)
        {
            _logger.LogDebug("Role maintenance job is currently running, skipping role integrity check");
            return;
        }

        bool memberRolesChanged = notification.EventArgs.RolesBefore.Count != notification.EventArgs.RolesAfter.Count;

        if (!memberRolesChanged) return;

        ulong[] memberRoleIds = _options.MemberRoleIds;
        ulong memberRoleId = _options.MemberRoleId;
        ulong ghostRoleId = _options.GhostRoleId;

        DiscordMember? member = notification.EventArgs.Member;

        if (member is null) throw new Exception(nameof(member));

        DiscordGuild guild = notification.EventArgs.Guild;

        await EnsureMemberRole(guild, memberRoleId, member, memberRoleIds);

        await EnsureGhostRole(guild, ghostRoleId, member);
    }

    private static async Task EnsureMemberRole(
        DiscordGuild guild,
        ulong memberRoleId,
        DiscordMember member,
        ulong[] memberRoleIds)
    {
        DiscordRole memberRole = guild.Roles[memberRoleId]
                                 ?? throw new Exception($"Could not find member role with id {memberRoleId}");

        bool userShouldHaveMemberRole = member.Roles.Any(r => memberRoleIds.Contains(r.Id));
        bool userHasMemberRole = member.Roles.Any(r => r.Id == memberRoleId);

        if (userShouldHaveMemberRole && !userHasMemberRole)
        {
            await member.GrantRoleAsync(memberRole);
        }
        else if (!userShouldHaveMemberRole && userHasMemberRole)
        {
            await member.RevokeRoleAsync(memberRole);
        }
    }

    private static async Task EnsureGhostRole(DiscordGuild guild, ulong ghostRoleId, DiscordMember member)
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
}

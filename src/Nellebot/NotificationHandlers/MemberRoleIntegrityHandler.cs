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
using Nellebot.Services;
using Nellebot.Utils;
using Quartz;

namespace Nellebot.NotificationHandlers;

public class MemberRoleIntegrityHandler : INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly ILogger<MemberRoleIntegrityHandler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly DiscordResolver _discordResolver;
    private readonly QuarantineService _quarantineService;
    private readonly BotOptions _options;

    public MemberRoleIntegrityHandler(
        ILogger<MemberRoleIntegrityHandler> logger,
        IOptions<BotOptions> options,
        ISchedulerFactory schedulerFactory,
        DiscordResolver discordResolver,
        QuarantineService quarantineService)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _discordResolver = discordResolver;
        _quarantineService = quarantineService;
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

        await MaintainMemberRole(guild, member, quarantineRoleId);

        await MaintainBeginnerRole(guild, member, quarantineRoleId);

        await MaintainGhostRole(guild, member);
    }

    private async Task MaintainMemberRole(
        DiscordGuild guild,
        DiscordMember member,
        ulong quarantineRoleId)
    {
        ulong memberRoleId = _options.MemberRoleId;
        ulong[] memberActivatingRoleIds = _options.MemberActivatingRoleIds;
        DiscordRole memberRole = guild.Roles[memberRoleId]
                                 ?? throw new Exception($"Could not find Member role with id {memberRoleId}");

        await MaintainActivatableRole(member, memberRole, memberActivatingRoleIds, quarantineRoleId);
    }

    private async Task MaintainBeginnerRole(
        DiscordGuild guild,
        DiscordMember member,
        ulong quarantineRoleId)
    {
        ulong beginnerRoleId = _options.BeginnerRoleId;
        ulong[] beginnerActivatingRoleIds = _options.BeginnerActivatingRoleIds;
        DiscordRole beginnerRole = guild.Roles[beginnerRoleId]
                                   ?? throw new Exception($"Could not find Beginner role with id {beginnerRoleId}");

        await MaintainActivatableRole(member, beginnerRole, beginnerActivatingRoleIds, quarantineRoleId);
    }

    /// <summary>
    /// Ensures that the activatable role is added if the user has any of the activating roles,
    /// and removed if the user has none of the activating roles
    /// </summary>
    private static async Task MaintainActivatableRole(
        DiscordMember member,
        DiscordRole activatableRole,
        ulong[] activatingRoleIds,
        ulong quarantineRoleId)
    {
        bool userHasActivatableRole = member.Roles.Any(r => r.Id == activatableRole.Id);
        bool userHasActivatingRoles = member.Roles.Any(r => activatingRoleIds.Contains(r.Id));
        bool userHasQuarantineRole = member.Roles.Any(r => r.Id == quarantineRoleId);

        bool userIsEligibleForActivatableRole = userHasActivatingRoles && !userHasQuarantineRole;

        if (!userHasActivatableRole && userIsEligibleForActivatableRole)
        {
            await member.GrantRoleAsync(activatableRole);
        }
        else if (userHasActivatableRole && !userIsEligibleForActivatableRole)
        {
            await member.RevokeRoleAsync(activatableRole);
        }
    }

    /// <summary>
    /// Ensures that the ghost role is added if the user has no roles,
    /// and removed if the user has any other roles
    /// </summary>
    private async Task MaintainGhostRole(DiscordGuild guild, DiscordMember member)
    {
        ulong ghostRoleId = _options.GhostRoleId;
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
        await _quarantineService.QuarantineMember(member, botMember, quarantineReason);
    }
}

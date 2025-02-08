using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Quartz;

namespace Nellebot.Jobs;

public class RoleMaintenanceJob : IJob
{
    public static readonly JobKey Key = new("role-maintenance", "default");

    private readonly DiscordClient _client;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;

    public RoleMaintenanceJob(
        IOptions<BotOptions> options,
        DiscordClient client,
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger)
    {
        _client = client;
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _options = options.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _discordLogger.LogExtendedActivityMessage($"Job started: {Key}");

            CancellationToken cancellationToken = context.CancellationToken;

            ulong guildId = _options.GuildId;
            ulong memberRoleId = _options.MemberRoleId;
            ulong[] memberRoleIds = _options.MemberRoleIds;
            ulong ghostRoleId = _options.GhostRoleId;

            DiscordGuild guild = _client.Guilds[guildId];

            _discordLogger.LogExtendedActivityMessage("Downloading guild members.");

            List<DiscordMember> allMembers = await guild.GetAllMembersAsync(cancellationToken).ToListAsync();

            _discordLogger.LogExtendedActivityMessage($"Downloaded {allMembers.Count} guild members.");

            DiscordRole memberRole = guild.Roles[memberRoleId]
                                     ?? throw new Exception($"Could not find member role with id {memberRoleId}");
            DiscordRole ghostRole = guild.Roles[ghostRoleId]
                                    ?? throw new Exception($"Could not find ghost role with id {ghostRoleId}");

            await AddMissingMemberRoles(allMembers, memberRoleIds, memberRole, cancellationToken);

            await RemoveUnneededMemberRoles(allMembers, memberRoleIds, memberRole, cancellationToken);

            await AddMissingGhostRoles(allMembers, ghostRole, cancellationToken);

            await RemoveUnneededGhostRoles(allMembers, ghostRole, cancellationToken);

            _discordLogger.LogExtendedActivityMessage($"Job finished: {Key}");
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, ex.Message);
            throw new JobExecutionException(ex);
        }
    }

    private async Task AddMissingMemberRoles(
        List<DiscordMember> allMembers,
        ulong[] memberRoleIds,
        DiscordRole memberRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> missingMemberRoleMembers = allMembers
            .Where(m => m.Roles.All(r => r.Id != memberRole.Id))
            .Where(m => m.Roles.Any(r => memberRoleIds.Contains(r.Id)))
            .ToList();

        if (missingMemberRoleMembers.Count != 0)
        {
            int totalCount = missingMemberRoleMembers.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {missingMemberRoleMembers.Count} users which are missing the Member role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                missingMemberRoleMembers,
                m => m.GrantRoleAsync(memberRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done adding Member role for {successCount}/{totalCount} users.");
        }
    }

    private async Task RemoveUnneededMemberRoles(
        List<DiscordMember> allMembers,
        ulong[] memberRoleIds,
        DiscordRole memberRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> memberRoleCandidates = allMembers
            .Where(
                m => !m.Roles.Any(r => memberRoleIds.Contains(r.Id))
                     && m.Roles.Any(r => r.Id == memberRole.Id))
            .ToList();

        if (memberRoleCandidates.Count != 0)
        {
            int totalCount = memberRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {memberRoleCandidates.Count} users with unneeded Member role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                memberRoleCandidates,
                m => m.RevokeRoleAsync(memberRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done removing Member role for {successCount}/{totalCount} users.");
        }
    }

    private async Task AddMissingGhostRoles(
        List<DiscordMember> allMembers,
        DiscordRole ghostRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> ghostRoleCandidates = allMembers
            .Where(m => !m.Roles.Any())
            .ToList();

        if (ghostRoleCandidates.Count != 0)
        {
            int totalCount = ghostRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {ghostRoleCandidates.Count} users which are missing the Ghost role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                ghostRoleCandidates,
                m => m.GrantRoleAsync(ghostRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done adding Ghost role for {successCount}/{totalCount} users.");
        }
    }

    private async Task RemoveUnneededGhostRoles(
        List<DiscordMember> allMembers,
        DiscordRole ghostRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> ghostRoleCandidates = allMembers
            .Where(m => m.Roles.Any(r => r.Id == ghostRole.Id) && m.Roles.Count() > 1)
            .ToList();

        if (ghostRoleCandidates.Count != 0)
        {
            int totalCount = ghostRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {ghostRoleCandidates.Count} users with unneeded Ghost role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                ghostRoleCandidates,
                m => m.RevokeRoleAsync(ghostRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done removing Ghost role for {successCount}/{totalCount} users.");
        }
    }

#pragma warning disable SA1204
    private static async Task<int> ExecuteRoleChangeWithRetry(
        List<DiscordMember> roleRecipients,
        Func<DiscordMember, Task> roleChangeFunc,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        const int roleChangeDelayMs = 100;
        const int retryDelayMs = 1000;

        foreach (DiscordMember member in roleRecipients)
        {
            var attempt = 0;

            while (attempt < 3)
            {
                attempt++;

                try
                {
                    await roleChangeFunc(member);

                    successCount++;

                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(retryDelayMs * attempt, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(roleChangeDelayMs, cancellationToken);
        }

        return successCount;
    }
}

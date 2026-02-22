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
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordLogger _discordLogger;
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
            _discordLogger.LogOperationMessage($"Job started: {Key}");

            CancellationToken cancellationToken = context.CancellationToken;

            ulong guildId = _options.GuildId;
            ulong quarantineRoleId = _options.QuarantineRoleId;

            DiscordGuild guild = _client.Guilds[guildId];

            _discordLogger.LogOperationMessage("Downloading guild members.");

            List<DiscordMember> allMembers = await guild.GetAllMembersAsync(cancellationToken).ToListAsync();

            _discordLogger.LogOperationMessage($"Downloaded {allMembers.Count} guild members.");

            await MaintainMemberRoles(guild, allMembers, quarantineRoleId, cancellationToken);

            await MaintainBeginnerRoles(guild, allMembers, quarantineRoleId, cancellationToken);

            _discordLogger.LogOperationMessage($"Job finished: {Key}");
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, ex.Message);
            throw new JobExecutionException(ex);
        }
    }

    private async Task MaintainMemberRoles(
        DiscordGuild guild,
        List<DiscordMember> allMembers,
        ulong quarantineRoleId,
        CancellationToken cancellationToken)
    {
        ulong memberRoleId = _options.MemberRoleId;
        ulong[] memberActivatingRoleIds = _options.MemberActivatingRoleIds;
        DiscordRole memberRole = guild.Roles[memberRoleId]
                                 ?? throw new Exception($"Could not find Member role with id {memberRoleId}");

        await AddMissingActivatableRoles(
            allMembers,
            memberRole,
            memberActivatingRoleIds,
            quarantineRoleId,
            cancellationToken);

        await RemoveUnneededActivatableRoles(
            allMembers,
            memberRole,
            memberActivatingRoleIds,
            quarantineRoleId,
            cancellationToken);
    }

    private async Task MaintainBeginnerRoles(
        DiscordGuild guild,
        List<DiscordMember> allMembers,
        ulong quarantineRoleId,
        CancellationToken cancellationToken)
    {
        ulong beginnerRoleId = _options.BeginnerRoleId;
        ulong[] beginnerActivatingRoleIds = _options.BeginnerActivatingRoleIds;
        DiscordRole beginnerRole = guild.Roles[beginnerRoleId]
                                   ?? throw new Exception($"Could not find Beginner role with id {beginnerRoleId}");

        await AddMissingActivatableRoles(
            allMembers,
            beginnerRole,
            beginnerActivatingRoleIds,
            quarantineRoleId,
            cancellationToken);

        await RemoveUnneededActivatableRoles(
            allMembers,
            beginnerRole,
            beginnerActivatingRoleIds,
            quarantineRoleId,
            cancellationToken);
    }

    private async Task AddMissingActivatableRoles(
        List<DiscordMember> allMembers,
        DiscordRole activatableRole,
        ulong[] activatingRoleIds,
        ulong quarantineRoleId,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> missingRoleMembers = allMembers
            .Where(m => !HasMemberRole(m) && HasActivatingRoles(m) && !HasQuarantineRole(m))
            .ToList();

        if (missingRoleMembers.Count == 0) return;

        int totalCount = missingRoleMembers.Count;

        _discordLogger.LogOperationMessage(
            $"Found {missingRoleMembers.Count} users which are missing the {activatableRole.Name} role.");

        int successCount = await ExecuteRoleChangeWithRetry(
            missingRoleMembers,
            m => m.GrantRoleAsync(activatableRole),
            cancellationToken);

        _discordLogger.LogOperationMessage(
            $"Done adding {activatableRole.Name} role for {successCount}/{totalCount} users.");

        return;

        bool HasActivatingRoles(DiscordMember m)
        {
            return m.Roles.Any(r => activatingRoleIds.Contains(r.Id));
        }

        bool HasMemberRole(DiscordMember m)
        {
            return m.Roles.Any(r => r.Id == activatableRole.Id);
        }

        bool HasQuarantineRole(DiscordMember m)
        {
            return m.Roles.Any(r => r.Id == quarantineRoleId);
        }
    }

    private async Task RemoveUnneededActivatableRoles(
        List<DiscordMember> allMembers,
        DiscordRole activatableRole,
        ulong[] activatingRoleIds,
        ulong quarantineRoleId,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> memberRoleCandidates = allMembers
            .Where(m => HasMemberRole(m) && (!HasActivatingRoles(m) || HasQuarantineRole(m)))
            .ToList();

        if (memberRoleCandidates.Count == 0) return;

        int totalCount = memberRoleCandidates.Count;

        _discordLogger.LogOperationMessage(
            $"Found {memberRoleCandidates.Count} users with unneeded {activatableRole.Name} role.");

        int successCount = await ExecuteRoleChangeWithRetry(
            memberRoleCandidates,
            m => m.RevokeRoleAsync(activatableRole),
            cancellationToken);

        _discordLogger.LogOperationMessage(
            $"Done removing {activatableRole.Name} role for {successCount}/{totalCount} users.");

        return;

        bool HasActivatingRoles(DiscordMember m)
        {
            return m.Roles.Any(r => activatingRoleIds.Contains(r.Id));
        }

        bool HasMemberRole(DiscordMember m)
        {
            return m.Roles.Any(r => r.Id == activatableRole.Id);
        }

        bool HasQuarantineRole(DiscordMember m)
        {
            return m.Roles.Any(r => r.Id == quarantineRoleId);
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
        const int maxAttempts = 3;

        foreach (DiscordMember member in roleRecipients)
        {
            var attempt = 0;

            while (attempt < maxAttempts)
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

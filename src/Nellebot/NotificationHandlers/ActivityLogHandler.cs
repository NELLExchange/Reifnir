using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Entities.AuditLogs;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Data.Repositories;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class ActivityLogHandler : INotificationHandler<GuildBanAddedNotification>,
    INotificationHandler<GuildBanRemovedNotification>,
    INotificationHandler<MessageDeletedNotification>,
    INotificationHandler<MessageBulkDeletedNotification>,
    INotificationHandler<GuildMemberAddedNotification>,
    INotificationHandler<GuildMemberRemovedNotification>,
    INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly BotOptions _botOptions;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordLogger _discordLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly MessageRefRepository _messageRefRepo;
    private readonly UserLogService _userLogService;

    public ActivityLogHandler(
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver,
        MessageRefRepository messageRefRepo,
        UserLogService userLogService,
        IOptions<BotOptions> botOptions)
    {
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _messageRefRepo = messageRefRepo;
        _userLogService = userLogService;
        _botOptions = botOptions.Value;
    }

    public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildBanAddedEventArgs args = notification.EventArgs;

        string memberName = args.Member.GetDetailedMemberIdentifier();

        var auditBanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>(
            args.Guild,
            DiscordAuditLogActionType.Ban,
            x => x.Target.Id == args.Member.Id);

        if (auditBanEntry == null) return;

        DiscordMember? memberResponsible =
            await _discordResolver.ResolveGuildMember(args.Guild, auditBanEntry.UserResponsible.Id);

        if (memberResponsible is null) return;

        string responsibleName = memberResponsible.DisplayName;

        _discordLogger.LogActivityMessage(
            $"**{memberName}** was banned by **{responsibleName}**. Reason: {auditBanEntry.Reason}.");
    }

    public async Task Handle(GuildBanRemovedNotification notification, CancellationToken cancellationToken)
    {
        GuildBanRemovedEventArgs args = notification.EventArgs;

        string memberName = args.Member.GetDetailedMemberIdentifier();

        var auditUnbanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>(
            args.Guild,
            DiscordAuditLogActionType.Unban,
            x => x.Target.Id == args.Member.Id);

        if (auditUnbanEntry == null) return;

        DiscordMember? memberResponsible =
            await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

        if (memberResponsible is null) return;

        string responsibleName = memberResponsible.DisplayName;

        _discordLogger.LogActivityMessage($"**{memberName}** was unbanned by **{responsibleName}**.");
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberAddedEventArgs args = notification.EventArgs;

        DiscordMember member = args.Member;

        string memberIdentifier = member.GetDetailedMemberIdentifier();

        _discordLogger.LogActivityMessage($"**{memberIdentifier}** joined the server");

        await _userLogService.CreateUserLog(member.Id, DateTime.UtcNow, UserLogType.JoinedServer);
        await _userLogService.CreateUserLog(member.Id, member.GetFullUsername(), UserLogType.UsernameChange);
    }

    public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberRemovedEventArgs args = notification.EventArgs;

        DiscordMember member = args.Member;
        DiscordGuild guild = args.Guild;

        string memberFullIdentifier = member.GetDetailedMemberIdentifier();

        // It's possible that the audit log entry might not be available right away.
        // If that turns out to be the case, consider wrapping this call into some sort of exeponential backoff retry.
        TryResolveResult<DiscordAuditLogKickEntry> auditResolveResult =
            await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogKickEntry>(
                args.Guild,
                DiscordAuditLogActionType.Kick,
                x => x.Target.Id == args.Member.Id);

        bool userWasKicked = auditResolveResult.Resolved;

        if (userWasKicked)
        {
            DiscordAuditLogKickEntry auditKickEntry = auditResolveResult.Value;

            DiscordMember? memberResponsible =
                await _discordResolver.ResolveGuildMember(guild, auditKickEntry.UserResponsible.Id);

            string kickReason = auditKickEntry.Reason.NullOrWhiteSpaceTo("*No reason provided*");

            if (memberResponsible is null) return;

            string responsibleName = memberResponsible.DisplayName;

            _discordLogger.LogActivityMessage(
                $"**{memberFullIdentifier}** was kicked by **{responsibleName}**. Reason: {kickReason}.");

            await _userLogService.CreateUserLog(
                member.Id,
                DateTime.UtcNow,
                UserLogType.LeftServer,
                memberResponsible.Id);
        }
        else
        {
            _discordLogger.LogActivityMessage($"**{memberFullIdentifier}** left the server");

            await _userLogService.CreateUserLog(member.Id, DateTime.UtcNow, UserLogType.LeftServer);
        }
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var totalChanges = 0;

        GuildMemberUpdatedEventArgs args = notification.EventArgs;

        int roleChanges = CheckForRolesUpdate(args);
        totalChanges += roleChanges;

        bool nicknameUpdated = await CheckForNicknameUpdate(args);
        if (nicknameUpdated) totalChanges++;

        bool usernameUpdated = await CheckForUsernameUpdate(args);
        if (usernameUpdated) totalChanges++;

        // Test if there actually are several changes in the same event
        if (totalChanges > 1)
        {
            _discordLogger.LogExtendedActivityMessage(
                $"{nameof(GuildMemberUpdatedNotification)} contained {totalChanges} changes");
        }
    }

    public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessagesBulkDeletedEventArgs args = notification.EventArgs;

        if (args.Messages.Count == 0)
        {
            _discordErrorLogger.LogWarning(
                $"{nameof(MessageBulkDeletedNotification)}",
                "Notification contained no messages.");
            return;
        }

        List<AppDiscordMessage> messages = await MapAndEnrichMessages(args.Messages);

        IEnumerable<IGrouping<ulong?, AppDiscordMessage>> messagesByAuthor = messages.GroupBy(m => m.Author?.Id);

        foreach (IGrouping<ulong?, AppDiscordMessage> authorMessages in messagesByAuthor)
        {
            AppDiscordUser author = authorMessages.First().Author;

            string authorName = author.GetDetailedUserIdentifier();

            DiscordAuditLogEntry? auditEntry;

            TryResolveResult<DiscordAuditLogMessageEntry> auditResolveResult =
                await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>(
                    args.Guild,
                    DiscordAuditLogActionType.MessageBulkDelete,
                    x => x.Target.Id == author.Id);

            if (auditResolveResult.Resolved)
            {
                auditEntry = auditResolveResult.Value;
            }
            else
            {
                // Could not find any audit log entry for the author
                _discordErrorLogger.LogWarning(
                    $"{nameof(MessageBulkDeletedNotification)}",
                    $"Could not find any audit log entry for {authorName} of type {DiscordAuditLogActionType.MessageBulkDelete}");

                // Check if the user was banned
                // It's possible that the audit log entry might not be available right away.
                // If that turns out to be the case, consider wrapping this call into some sort of exponential backoff retry.
                var auditBanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>(
                    args.Guild,
                    DiscordAuditLogActionType.Ban,
                    x => x.Target.Id == author.Id);

                auditEntry = auditBanEntry;

                if (auditBanEntry is null)
                {
                    // Could not find any audit log entry for the author
                    _discordErrorLogger.LogWarning(
                        $"{nameof(MessageBulkDeletedNotification)}",
                        $"Could not find any audit log entry for {authorName} of type {DiscordAuditLogActionType.Ban}");
                }
            }

            var responsibleName = "Unknown mod";

            if (auditEntry?.UserResponsible is not null)
            {
                DiscordMember? memberResponsible =
                    await _discordResolver.ResolveGuildMember(args.Guild, auditEntry.UserResponsible.Id);

                responsibleName = memberResponsible?.DisplayName ?? responsibleName;
            }

            _discordLogger.LogActivityMessage(
                $"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            var sb = new StringBuilder();

            sb.AppendLine(
                $"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            foreach (AppDiscordMessage message in messages.Where(x => x != null))
            {
                sb.AppendLine();
                sb.AppendLine($"In {message.Channel.Name} at {message.CreationTimestamp}:");

                if (!string.IsNullOrWhiteSpace(message.Content)) sb.AppendLine($"> {message.Content}");
            }

            _discordLogger.LogExtendedActivityMessage(sb.ToString());
        }
    }

    public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageDeletedEventArgs args = notification.EventArgs;

        DiscordGuild guild = args.Guild;
        DiscordChannel channel = args.Channel;
        DiscordMessage deletedMessage = args.Message;

        if (channel.IsPrivate || channel.Id == _botOptions.ActivityLogChannelId ||
            channel.Id == _botOptions.ExtendedActivityLogChannelId)
        {
            return;
        }

        if (deletedMessage.Author?.IsBot ?? false) return;

        AppDiscordMessage? message = await MapAndEnrichMessage(deletedMessage);

        if (message == null)
        {
            _discordErrorLogger.LogWarning(
                $"{nameof(MessageDeletedNotification)}",
                $"Could not resolve message id {deletedMessage.Id}");

            _discordLogger.LogExtendedActivityMessage($"An unknown message in **{channel.Name}** was removed");

            return;
        }

        // The target is supposed to be a Message but the id corresponds to a user.
        // And it's not even the id of the message author, but the id of the user who deleted the message.
        // This is a bug in the Discord API or in DSharp. Probably.
        // Therefore, try to guess by finding a recent MessageDelete audit log in the same channel.
        // Also, subsequent deletions of messages from the same author, in the same channel do not create new audit log entries,
        // and instead increment the count of the original entry, so use a generous max age.
        // Thanks for coming to my TED talk.
        const int maxAuditLogAgeMinutes = 60;
        TryResolveResult<DiscordAuditLogMessageEntry> auditResolveResult =
            await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>(
                guild,
                DiscordAuditLogActionType.MessageDelete,
                x => x.Channel.Id == message.Channel.Id,
                maxAuditLogAgeMinutes);

        DiscordAuditLogMessageEntry? auditMessageDeleteEntry = null;

        if (!auditResolveResult.Resolved)
        {
            // User, likely, deleted their own message
            return;
        }

        auditMessageDeleteEntry = auditResolveResult.Value;

        if (auditMessageDeleteEntry?.UserResponsible is null)
        {
            _discordErrorLogger.LogWarning(
                $"{nameof(MessageDeletedNotification)}",
                "Could not find any responsible user for the message delete event.");
            return;
        }

        if (auditMessageDeleteEntry.UserResponsible.Id == message.Author.Id)
        {
            // User deleted their own message
            return;
        }

        DiscordMember? memberResponsible = null;

        if (auditMessageDeleteEntry?.UserResponsible is not null)
        {
            memberResponsible =
                await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);
        }

        string responsibleName = memberResponsible?.DisplayName ?? "Unknown mod";

        DiscordMember? authorAsMember = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        string authorName = authorAsMember?.GetDetailedMemberIdentifier() ?? "Unknown user";

        var logMessage =
            $"Message written by **{authorName}** in **{channel.Name}** was removed by **{responsibleName}**.";

        _discordLogger.LogActivityMessage(logMessage);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            logMessage += $" Original message:{Environment.NewLine}> {message.Content}";
        }

        _discordLogger.LogExtendedActivityMessage(logMessage);
    }

    private async Task<bool> CheckForUsernameUpdate(GuildMemberUpdatedEventArgs args)
    {
        string? usernameAfter = args.MemberAfter.GetFullUsername();
        string? usernameBefore = args.MemberBefore?.GetFullUsername();

        if (string.IsNullOrWhiteSpace(usernameBefore) || usernameBefore == usernameAfter)
        {
            usernameBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.UsernameChange))
                ?.GetValue<string>();
        }

        if (usernameBefore == usernameAfter) return false;

        _discordLogger.LogExtendedActivityMessage(
            $"Username change for {args.Member.Mention}. {usernameBefore ?? "??"} => {usernameAfter ?? "??"}.");

        await _userLogService.CreateUserLog(args.Member.Id, usernameAfter, UserLogType.UsernameChange);

        return true;
    }

    private async Task<bool> CheckForNicknameUpdate(GuildMemberUpdatedEventArgs args)
    {
        string? nicknameAfter = args.NicknameAfter;
        string? nicknameBefore = args.NicknameBefore;

        if (string.IsNullOrWhiteSpace(nicknameBefore) || nicknameBefore == nicknameAfter)
        {
            nicknameBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.NicknameChange))
                ?.GetValue<string>();
        }

        // TODO check if member's nickname was changed by moderator
        if (nicknameBefore == nicknameAfter) return false;

        var message =
            $"Nickname change for {args.Member.Mention}. {nicknameBefore ?? "*no nickname*"} => {nicknameAfter ?? "*no nickname*"}.";

        _discordLogger.LogExtendedActivityMessage(message);

        await _userLogService.CreateUserLog(args.Member.Id, nicknameAfter, UserLogType.NicknameChange);

        return true;
    }

    private int CheckForRolesUpdate(GuildMemberUpdatedEventArgs args)
    {
        List<DiscordRole> addedRoles = args.RolesAfter.ExceptBy(args.RolesBefore.Select(r => r.Id), x => x.Id).ToList();
        List<DiscordRole> removedRoles =
            args.RolesBefore.ExceptBy(args.RolesAfter.Select(r => r.Id), x => x.Id).ToList();

        string memberMention = args.Member.Mention;
        string memberDisplayName = args.Member.DisplayName;
        string memberDetailedIdentifier = args.Member.GetDetailedMemberIdentifier(true);

        int roleChangesCount = addedRoles.Count + removedRoles.Count;

        var warningMessage = new StringBuilder();

        if (addedRoles.Count > 0)
        {
            string addedRolesNames = string.Join(", ", addedRoles.Select(r => r.Name));
            _discordLogger.LogActivityMessage($"Added roles to **{memberDisplayName}**: {addedRolesNames}");

            foreach (DiscordRole addedRole in addedRoles)
            {
                _discordLogger.LogExtendedActivityMessage($"Role change for {memberMention}: Added {addedRole.Name}.");

                if (addedRole.Id == _botOptions.SpammerRoleId)
                {
                    warningMessage.AppendLine($"Awoooooo! **{memberDetailedIdentifier}** is a **{addedRole.Name}**.");
                }
            }
        }

        if (removedRoles.Count > 0)
        {
            string removedRolesNames = string.Join(", ", removedRoles.Select(r => r.Name));
            _discordLogger.LogActivityMessage($"Removed roles from **{memberDisplayName}**: {removedRolesNames}");

            foreach (DiscordRole removedRole in removedRoles)
            {
                _discordLogger.LogExtendedActivityMessage(
                    $"Role change for {memberMention}: Removed {removedRole.Name}.");
            }
        }

        if (addedRoles.Count > 2)
        {
            warningMessage.AppendLine(
                $"Awoooooo! **{memberDetailedIdentifier}** chose more than 2 roles in one go. Possibly bot.");
        }

        if (warningMessage.Length > 0) _discordLogger.LogTrustedChannelMessage(warningMessage.ToString().TrimEnd());

        return roleChangesCount;
    }

    private async Task<AppDiscordMessage?> MapAndEnrichMessage(DiscordMessage deletedMessage)
    {
        AppDiscordMessage appDiscordMessage = DiscordMessageMapper.Map(deletedMessage);

        bool isCompletedMessage =
            !string.IsNullOrWhiteSpace(appDiscordMessage.Content) && appDiscordMessage.Author != null;

        if (isCompletedMessage) return appDiscordMessage;

        MessageRef? messageRef = await _messageRefRepo.GetMessageRef(deletedMessage.Id);

        if (messageRef == null) return null;

        DiscordMember? authorAsGuildMember = await _discordResolver.ResolveGuildMember(messageRef.UserId);

        appDiscordMessage.Author = authorAsGuildMember is not null
            ? DiscordMemberMapper.Map(authorAsGuildMember)
            : AppDiscordMember.BuildStub(messageRef.UserId);

        return appDiscordMessage;
    }

    private async Task<List<AppDiscordMessage>> MapAndEnrichMessages(IEnumerable<DiscordMessage> deletedMessages)
    {
        List<AppDiscordMessage> mappedMessages = (deletedMessages.Select(DiscordMessageMapper.Map) ?? [])
            .ToList();

        List<AppDiscordMessage> completeMessages = mappedMessages
            .Where(m => !string.IsNullOrWhiteSpace(m.Content) && m.Author != null)
            .ToList();

        List<AppDiscordMessage> incompleteMessages = mappedMessages
            .ExceptBy(completeMessages.Select(x => x.Id), m => m.Id)
            .ToList();

        List<MessageRef> messageRefs =
            (await _messageRefRepo.GetMessageRefs(incompleteMessages.Select(m => m.Id).ToArray()))
            .ToList();

        foreach (AppDiscordMessage message in incompleteMessages)
        {
            MessageRef? messageRef = messageRefs.FirstOrDefault(mr => mr.MessageId == message.Id);

            if (messageRef == null) continue;

            DiscordMember? authorAsGuildMember = await _discordResolver.ResolveGuildMember(messageRef.UserId);

            message.Author = authorAsGuildMember is not null
                ? DiscordMemberMapper.Map(authorAsGuildMember)
                : AppDiscordMember.BuildStub(messageRef.UserId);

            completeMessages.Add(message);
        }

        return completeMessages;
    }
}

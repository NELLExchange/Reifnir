using System;
using System.Collections.Generic;
using DSharpPlus.EventArgs;
using MediatR;

namespace Nellebot.NotificationHandlers;

public abstract record EventNotification : INotification;

#pragma warning disable SA1402 // File may only contain a single type
public record GuildMemberUpdatedNotification(GuildMemberUpdatedEventArgs EventArgs) : EventNotification;

public record GuildMemberAddedNotification(GuildMemberAddedEventArgs EventArgs) : EventNotification;

public record GuildMemberRemovedNotification(GuildMemberRemovedEventArgs EventArgs) : EventNotification;

public record GuildBanAddedNotification(GuildBanAddedEventArgs EventArgs) : EventNotification;

public record GuildBanRemovedNotification(GuildBanRemovedEventArgs EventArgs) : EventNotification;

public record MessageReactionAddedNotification(MessageReactionAddedEventArgs EventArgs) : EventNotification;

public record MessageReactionRemovedNotification(MessageReactionRemovedEventArgs EventArgs) : EventNotification;

public record MessageCreatedNotification(MessageCreatedEventArgs EventArgs) : EventNotification;

public record MessageUpdatedNotification(MessageUpdatedEventArgs EventArgs) : EventNotification;

public record MessageDeletedNotification(MessageDeletedEventArgs EventArgs) : EventNotification;

public record MessageBulkDeletedNotification(MessagesBulkDeletedEventArgs EventArgs) : EventNotification;

public record ClientHeartbeatNotification(DateTime Timestamp, TimeSpan Ping) : EventNotification;

public record SessionCreatedNotification() : EventNotification;

public record SessionResumedOrDownloadCompletedNotification(string EventSource) : EventNotification;

public record ClientConnected(SocketOpenedEventArgs EventArgs) : EventNotification;

public record ClientDisconnected(SocketClosedEventArgs EventArgs) : EventNotification;

public record BufferedMemberLeftNotification(IEnumerable<string> Usernames) : EventNotification;

using System;

namespace Nellebot;

public class BotOptions
{
    public const string OptionsKey = "Nellebot";

    public string CommandPrefix { get; init; } = null!;

    public string ConnectionString { get; init; } = null!;

    public string BotToken { get; init; } = null!;

    public ulong GuildId { get; init; }

    /// <summary>
    ///     Gets discord role with access to admin commands.
    /// </summary>
    public ulong ModRoleId { get; init; }

    /// <summary>
    ///     Gets the most shameful discord role.
    /// </summary>
    public ulong SpammerRoleId { get; init; }

    /// <summary>
    ///     Gets discord roles with access to some admin commands.
    /// </summary>
    public ulong[] TrustedRoleIds { get; init; } = Array.Empty<ulong>();

    /// <summary>
    ///     Gets channel id where Trusted roles have access to.
    /// </summary>
    public ulong TrustedChannelId { get; init; }

    public ulong ActivityLogChannelId { get; init; }

    public ulong ExtendedActivityLogChannelId { get; init; }

    public ulong GreetingsChannelId { get; init; }

    public ulong ErrorLogChannelId { get; init; }

    public ulong SuggestionsChannelId { get; init; }

    public ulong AwardChannelId { get; init; }

    public ulong[] AwardVoteGroupIds { get; init; } = [];

    public int RequiredAwardCount { get; init; }

    public ulong MemberRoleId { get; init; }

    public ulong[] MemberRoleIds { get; init; } = [];

    public ulong GhostRoleId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether feature flag for populating message refs on Ready event.
    /// </summary>
    public bool AutoPopulateMessagesOnReadyEnabled { get; init; }

    public ulong ModmailChannelId { get; init; }

    public int ModmailTicketInactiveThresholdInHours { get; init; }

    public int ValhallKickMaxMemberAgeInHours { get; init; }
}

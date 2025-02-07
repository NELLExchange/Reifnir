using System;

namespace Nellebot.Common.Models;

public class AwardMessage
{
    public Guid Id { get; set; }

    public ulong OriginalMessageId { get; set; }

    public ulong OriginalChannelId { get; set; }

    // Discord message id in awards channel
    public ulong AwardedMessageId { get; set; }

    public ulong AwardChannelId { get; set; }

    // Mainly for statistics reasons
    public ulong UserId { get; set; }

    public DateTime DateTime { get; set; }

    public uint AwardCount { get; set; }
}

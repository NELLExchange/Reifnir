using System;

namespace Nellebot.Common.Models;

public class MessageRef
{
    public ulong MessageId { get; set; }

    public ulong ChannelId { get; set; }

    public ulong UserId { get; set; }

    public DateTime DateTime { get; set; }
}

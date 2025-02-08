using System.Collections.Generic;

namespace Nellebot.Common.Models;

public class UserAwardStats
{
    public uint TotalAwardCount { get; set; }

    public uint AwardMessageCount { get; set; }

    public List<AwardMessage> TopAwardedMessages { get; set; } = null!;
}

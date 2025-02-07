using System;

namespace Nellebot.Common.Models;

public class BotSettting
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}

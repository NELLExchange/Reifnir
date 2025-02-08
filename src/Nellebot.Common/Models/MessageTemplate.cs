using System;

namespace Nellebot.Common.Models;

public class MessageTemplate
{
    public required string Id { get; set; }

    public required string Message { get; set; }

    public required string Type { get; set; }

    public ulong AuthorId { get; set; }

    public DateTime DateTime { get; set; }
}

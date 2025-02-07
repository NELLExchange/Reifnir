using System.Collections.Generic;

namespace Nellebot.Common.Models.Ordbok.Store;

public record OrdbokConceptStore
{
    public string Dictionary { get; init; } = null!;

    public Dictionary<string, string> Concepts { get; set; } = new();
}

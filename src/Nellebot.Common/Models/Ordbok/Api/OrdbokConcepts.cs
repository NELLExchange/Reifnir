using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public record OrdbokConcepts
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("concepts")]
    public Dictionary<string, OrdbokConcept> Concepts { get; set; } = new();
}

public record OrdbokConcept
{
    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("expansion")]
    public string Expansion { get; set; } = null!;
}

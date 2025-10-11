using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public record OrdbokSuggestResponse
{
    [JsonPropertyName("q")]
    public string Query { get; set; } = null!;

    [JsonPropertyName("cnt")]
    public int Count { get; set; }

    [JsonPropertyName("cmatch")]
    public int CountMatch { get; set; }

    [JsonPropertyName("a")]
    public Dictionary<string, Tupleish[]> SuggestionResults { get; set; } = new();
}

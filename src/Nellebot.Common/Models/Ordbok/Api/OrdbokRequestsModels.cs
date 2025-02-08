using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public class OrdbokSearchRequest
{
    [JsonPropertyName("dictionary_id")]
    public string DictionaryId { get; set; } = null!;

    [JsonPropertyName("q")]
    public string Query { get; set; } = null!;
}

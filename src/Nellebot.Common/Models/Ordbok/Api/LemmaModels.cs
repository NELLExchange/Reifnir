using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public record Lemma
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("lemma")]
    public string Value { get; set; } = null!;

    [JsonPropertyName("hgno")]
    public int HgNo { get; set; }

    [JsonPropertyName("initial_lexeme")]
    public string InitialLexeme { get; set; } = null!;

    [JsonPropertyName("final_lexeme")]
    public string FinalLexeme { get; set; } = null!;

    [JsonPropertyName("paradigm_info")]
    public List<Paradigm> Paradigms { get; set; } = new();
}

public record Inflection
{
    [JsonPropertyName("word_form")]
    public string WordForm { get; set; } = null!;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public record Paradigm
{
    [JsonPropertyName("paradigm_id")]
    public int ParadigmId { get; set; }

    [JsonPropertyName("inflection_group")]
    public string InflectionGroup { get; set; } = null!;

    [JsonPropertyName("standardisation")]
    public string Standardisation { get; set; } = null!;

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [JsonPropertyName("from")]
    public DateTime? From { get; set; }

    [JsonPropertyName("to")]
    public DateTime? To { get; set; }

    [JsonPropertyName("inflection")]
    public List<Inflection> Inflection { get; set; } = new();
}

public record SimpleLemma
{
    [JsonPropertyName("lemma")]
    public string Value { get; set; } = null!;

    [JsonPropertyName("hgno")]
    public int HgNo { get; set; }
}

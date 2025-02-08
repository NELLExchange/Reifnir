using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Converters;

namespace Nellebot.Common.Models.Ordbok.Api;

[JsonConverter(typeof(EtymologyGroupConverter))]
public abstract record EtymologyGroup : ITypeElement
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

// EtymologyLanguage
public record EtymologyLanguage : EtymologyGroup
{
    [JsonPropertyName("items")]
    public List<EtymologyLanguageElement> EtymologyLanguageElements { get; set; } = new();
}

[JsonConverter(typeof(EtymologyLanguageElementConverter))]
public abstract record EtymologyLanguageElement : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

/// <summary>
///     Api types so far: relation, language, grammar.
/// </summary>
public record EtymologyLanguageIdElement : EtymologyLanguageElement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

/// <summary>
///     Api types so far: usage.
/// </summary>
public record EtymologyLanguageTextElement : EtymologyLanguageElement
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

// EtymologyReference
public record EtymologyReference : EtymologyGroup
{
    [JsonPropertyName("items")]
    public List<EtymologyReferenceElement> EtymologyReferenceElements { get; set; } = new();
}

[JsonConverter(typeof(EtymologyReferenceElementConverter))]
public abstract record EtymologyReferenceElement : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

// Api types so far: relation, entity, grammar
public record EtymologyReferenceIdElement : EtymologyReferenceElement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

public record EtymologyReferenceArticleRef : EtymologyReferenceElement
{
    [JsonPropertyName("article_id")]
    public int ArticleId { get; set; }

    [JsonPropertyName("definition_id")]
    public int? DefinitionId { get; set; }

    [JsonPropertyName("lemmas")]
    public List<SimpleLemma> Lemmas { get; set; } = new();
}

// Etymology litt?
public record EtymologyLitt : EtymologyGroup
{
    [JsonPropertyName("items")]
    public List<EtymologyLittElement> EtymologyLittElements { get; set; } = new();
}

[JsonConverter(typeof(EtymologyLittElementConverter))]
public abstract record EtymologyLittElement : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

/// <summary>
///     Api types so far: entity.
/// </summary>
public record EtymologyLittIdElement : EtymologyLittElement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

/// <summary>
///     Api types so far: usage.
/// </summary>
public record EtymologyLittTextElement : EtymologyLittElement
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

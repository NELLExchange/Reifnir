using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nellebot.Common.Models.Ordbok.Converters;

namespace Nellebot.Common.Models.Ordbok.Api;

[JsonConverter(typeof(DefinitionElementConverter))]
public abstract record DefinitionElement : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

public record Definition : DefinitionElement
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("elements")]
    public List<DefinitionElement> DefinitionElements { get; set; } = new();
}

public record Explanation : DefinitionElement
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<ExplanationItem> ExplanationItems { get; set; } = new();
}

[JsonConverter(typeof(ExplanationItemConverter))]
public abstract record ExplanationItem : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

/// <summary>
///     Api types so far: relation, domain, entity, modifier, grammar, rhetoric, language, temporal.
/// </summary>
public record ExplanationIdItem : ExplanationItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

/// <summary>
///     Api types so far: usage.
/// </summary>
public record ExplanationTextItem : ExplanationItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

public record ExplanationArticleRefItem : ExplanationItem
{
    [JsonPropertyName("article_id")]
    public int ArticleId { get; set; }

    [JsonPropertyName("definition_order")]
    public int DefinitionOrder { get; set; }

    [JsonPropertyName("lemmas")]
    public List<SimpleLemma> Lemmas { get; set; } = new();
}

public record Example : DefinitionElement
{
    [JsonPropertyName("quote")]
    public Quote Quote { get; set; } = null!;

    [JsonPropertyName("explanation")]
    public ExampleExplanation Explanation { get; set; } = null!;
}

public record Quote
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("items")]
    public List<QuoteItem> QuoteItems { get; set; } = new();
}

[JsonConverter(typeof(QuoteItemConverter))]
public abstract record QuoteItem : ITypeElement
{
    [JsonPropertyName("type_")]
    public string Type { get; set; } = null!;
}

public record QuoteIdItem : QuoteItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}

public record QuoteTextItem : QuoteItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

public record ExampleExplanation
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

public record DefinitionSubArticle : DefinitionElement
{
    [JsonPropertyName("article_id")]
    public int ArticleId { get; set; }

    [JsonPropertyName("article")]
    public SubArticle Article { get; set; } = null!;
}

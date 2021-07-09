﻿using Nellebot.Common.Models.Ordbok.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok.Api
{
    public interface ITypeElement
    {
        public string Type { get; set; }
    }

    public class OrdbokSearchResponse : List<Article> { }

    public class Article
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }
        [JsonPropertyName("dictionary")]
        public string Dictionary { get; set; } = string.Empty;
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        [JsonPropertyName("body")]
        public Body Body { get; set; } = null!;
        [JsonPropertyName("lemmas")]
        public List<Lemma> Lemmas { get; set; } = new List<Lemma>();
    }

    public class Body
    {
        [JsonPropertyName("definitions")]
        public List<DefinitionGroup> DefinitionGroups { get; set; } = new List<DefinitionGroup>();
        [JsonPropertyName("etymology")]
        public List<EtymologyGroup> EtymologyGroups { get; set; } = new List<EtymologyGroup>();
    }

    #region Definitions

    public class DefinitionGroup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("elements")]
        public List<Definition> Definitions { get; set; } = new List<Definition>();
    }

    public class Definition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("elements")]
        public List<DefinitionElement> DefinitionElements { get; set; } = new List<DefinitionElement>();
    }


    [JsonConverter(typeof(DefinitionElementConverter))]
    public abstract class DefinitionElement : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class Explanation : DefinitionElement
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        // Items?
    }

    public class TextItem
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class ExampleExplanation
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        // items?
    }

    public class Quote
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<TextItem> Items { get; set; } = new List<TextItem>();
    }

    public class Example : DefinitionElement
    {
        [JsonPropertyName("quote")]
        public Quote Quote { get; set; } = null!;
        [JsonPropertyName("explanation")]
        public ExampleExplanation Explanation { get; set; } = null!;
    }

    #endregion

    #region etymologies


    [JsonConverter(typeof(EtymologyGroupConverter))]
    public abstract class EtymologyGroup: ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class EtymologyLanguage: EtymologyGroup
    {
        [JsonPropertyName("items")]
        public List<EtymologyLanguageElement> EtymologyLanguageElements { get; set; } = new List<EtymologyLanguageElement>();
    }

    [JsonConverter(typeof(EtymologyLanguageElementConverter))]
    public abstract class EtymologyLanguageElement : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class EtymologyLanguageLanguage : EtymologyLanguageElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyLanguageRelation : EtymologyLanguageElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyLanguageUsage : EtymologyLanguageElement
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class EtymologyReference: EtymologyGroup
    {
        [JsonPropertyName("items")]
        public List<EtymologyReferenceElement> EtymologyReferenceElements { get; set; } = new List<EtymologyReferenceElement>();
    }

    [JsonConverter(typeof(EtymologyReferenceElementConverter))]
    public abstract class EtymologyReferenceElement: ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class EtymologyReferenceRelation: EtymologyReferenceElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyReferenceArticleRef : EtymologyReferenceElement
    {
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
    }

    #endregion

    #region lemmas

    public class Lemma
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("lemma")]
        public string Value { get; set; } = string.Empty;
        [JsonPropertyName("hgno")]
        public int HgNo { get; set; }
        [JsonPropertyName("initial_lexeme")]
        public string InitialLexeme { get; set; } = string.Empty;
        [JsonPropertyName("final_lexeme")]
        public string FinalLexeme { get; set; } = string.Empty;
        [JsonPropertyName("paradigm_info")]
        public List<Paradigm> Paradigms { get; set; } = new List<Paradigm>();
    }

    public class Inflection
    {
        [JsonPropertyName("word_form")]
        public string WordForm { get; set; } = string.Empty;
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class Paradigm
    {
        [JsonPropertyName("paradigm_id")]
        public int ParadigmId { get; set; }
        [JsonPropertyName("inflection_group")]
        public string InflectionGroup { get; set; } = string.Empty;
        [JsonPropertyName("standardisation")]
        public string Standardisation { get; set; } = string.Empty;
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        [JsonPropertyName("from")]
        public DateTime? From { get; set; }
        [JsonPropertyName("to")]
        public DateTime? To { get; set; }
        [JsonPropertyName("inflection")]
        public List<Inflection> Inflection { get; set; } = new List<Inflection>();
    }

    #endregion

}

﻿using System.Collections.Generic;
using System.Linq;

namespace Nellebot.Common.Models.Ordbok.ViewModels;

public record Article
{
    public int ArticleId { get; set; }

    public List<Lemma> Lemmas { get; set; } = new List<Lemma>();

    public List<Definition> Definitions { get; set; } = new List<Definition>();

    public List<SubArticle> SubArticles { get; set; } = new List<SubArticle>();

    public List<Etymology> Etymologies { get; set; } = new List<Etymology>();

    public ParadigmV2 Paradigm { get; set; } = null!;
}

public record Lemma
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public int HgNo { get; set; }

    public string HgNoRoman { get; set; } = null!;

    public List<Paradigm> Paradigms { get; set; } = new List<Paradigm>();

    public List<string> UniqueParadigmValues => Paradigms.Select(p => p.Value).Distinct().ToList();
}

public record ParadigmV2
{
    public string WordClass { get; set; } = null!;

    public string? InflectionClass { get; set; }
}

public record Paradigm
{
    public string Value { get; set; } = null!;
}

public record Definition
{
    public List<string> Explanations { get; set; } = new List<string>();

    public List<string> ExplanationsSimple { get; set; } = new List<string>();

    public List<string> Examples { get; set; } = new List<string>();

    public List<Definition> InnerDefinitions { get; set; } = new List<Definition>();
}

public record SubArticle
{
    public List<Lemma> Lemmas { get; set; } = new List<Lemma>();

    public List<Definition> Definitions { get; set; } = new List<Definition>();
}

public record Etymology
{
    public string Content { get; set; } = null!;
}

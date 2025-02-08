using System.Collections.Generic;
using System.Linq;
using Nellebot.Common.Extensions;
using Api = Nellebot.Common.Models.Ordbok.Api;
using Vm = Nellebot.Common.Models.Ordbok.ViewModels;

namespace Nellebot.Services.Ordbok;

public class OrdbokModelMapper
{
    private readonly IOrdbokContentParser _contentParser;
    private readonly ILocalizationService _localizationService;

    public OrdbokModelMapper(IOrdbokContentParser contentParser, ILocalizationService localizationService)
    {
        _contentParser = contentParser;
        _localizationService = localizationService;
    }

    public Vm.Article MapArticle(Api.Article article, string dictionary)
    {
        var vmResult = new Vm.Article();

        vmResult.ArticleId = article.ArticleId;

        vmResult.Lemmas = article.Lemmas.Select(MapLemma).ToList();

        vmResult.Definitions = MapDefinitions(article.Body.DefinitionElements, dictionary);

        vmResult.SubArticles = MapSubArticles(article.Body.DefinitionElements, dictionary);

        vmResult.Etymologies = MapEtymologies(article.Body.EtymologyGroups, dictionary);

        vmResult.Paradigm = MapParadigmV2(article.Lemmas, dictionary);

        return vmResult;
    }

    public Vm.Lemma MapLemma(Api.Lemma lemma)
    {
        var vmResult = new Vm.Lemma();

        vmResult.Id = lemma.Id;
        vmResult.Value = lemma.Value;
        vmResult.HgNo = lemma.HgNo;
        vmResult.HgNoRoman = lemma.HgNo.ToRomanNumeral();
        vmResult.Paradigms = lemma.Paradigms.Select(MapParadigm).ToList();

        return vmResult;
    }

    public Vm.Paradigm MapParadigm(Api.Paradigm paradigm)
    {
        var vmResult = new Vm.Paradigm();

        if (!string.IsNullOrWhiteSpace(paradigm.InflectionGroup))
        {
            // TODO fix this
            vmResult.Value = paradigm.InflectionGroup.ToLower() switch
            {
                "noun" => $"{paradigm.Tags[1].ToLower()[0]}",
                "noun_regular" => $"{paradigm.Tags[1].ToLower()[0]}",
                "verb" => "v2",
                "verb_regular" => "v2",
                "adj_regular" => "adj.",
                "adj" => "adj.",
                "adv" => "adv.",
                "det_simple" => "det.",
                "pron" => "pron.",
                "sym" => "symb.",
                "intj" => "interj.",
                "abbr" => "fork.",
                _ => $"?{paradigm.InflectionGroup.ToLower()}?",
            };
        }

        return vmResult;
    }

    public Vm.ParadigmV2 MapParadigmV2(List<Api.Lemma> lemmas, string dictionary)
    {
        var vmResult = new Vm.ParadigmV2();

        if (lemmas == null || !lemmas.Any()) return vmResult;

        List<Api.Paradigm>? paradigms = lemmas.First().Paradigms;

        if (paradigms == null || !paradigms.Any()) return vmResult;

        string inflectionGroup = paradigms.First().InflectionGroup;

        inflectionGroup = inflectionGroup.Split("_")[0].ToLower(); // make noun_regular to noun, det_simple to det, etc.

        string? inflectionClass = null;

        string[] uniqueLevel1Tags = paradigms.Where(x => x.Tags.Length > 1).Select(x => x.Tags[1]).Distinct().ToArray();

        if (uniqueLevel1Tags.Length > 0)
        {
            if (inflectionGroup == "noun" && uniqueLevel1Tags.Length == 2)
            {
                inflectionClass = "masc_or_fem";
            }
            else
            {
                inflectionClass = uniqueLevel1Tags[0].ToLower();
            }
        }

        vmResult.WordClass = _localizationService.GetString(inflectionGroup, LocalizationResource.Ordbok, dictionary);
        if (inflectionClass != null)
        {
            vmResult.InflectionClass =
                _localizationService.GetString(inflectionClass, LocalizationResource.Ordbok, dictionary);
        }

        return vmResult;
    }

    public List<Vm.Definition> MapDefinitions(List<Api.DefinitionElement> definitionElements, string dictionary)
    {
        var vmResult = new List<Vm.Definition>();

        // TODO go recursive if a new nested level is discovered
        foreach (Api.DefinitionElement definitionElement in definitionElements)
        {
            // Top level element is always a Definition (hopefully)
            var definition = (Api.Definition)definitionElement;

            bool childrenAreDefinitions = definition.DefinitionElements.All(de => de is Api.Definition);

            if (childrenAreDefinitions)
            {
                List<Api.Definition> nestedDefinitions = definition.DefinitionElements.Cast<Api.Definition>().ToList();

                foreach (Api.Definition? nestedDefinition in nestedDefinitions)
                {
                    List<Api.DefinitionElement> nestedDefinitionElements = nestedDefinition.DefinitionElements
                        .Where(x => !(x is Api.DefinitionSubArticle))
                        .ToList();

                    Vm.Definition mappedNestedDefinition = MapDefinition(nestedDefinitionElements, dictionary);

                    List<Vm.Definition> innerDefinitions = nestedDefinitionElements
                        .Where(d => d is Api.Definition)
                        .Cast<Api.Definition>()
                        .Select(d => MapDefinition(d.DefinitionElements, dictionary))
                        .ToList();

                    mappedNestedDefinition.InnerDefinitions.AddRange(innerDefinitions);

                    vmResult.Add(mappedNestedDefinition);
                }
            }
            else
            {
                List<Api.DefinitionElement> nestedDefinitionElements = definition.DefinitionElements
                    .Where(x => !(x is Api.DefinitionSubArticle))
                    .ToList();

                Vm.Definition mappedDefinition = MapDefinition(nestedDefinitionElements, dictionary);

                vmResult.Add(mappedDefinition);
            }
        }

        return vmResult;
    }

    public List<Vm.SubArticle> MapSubArticles(List<Api.DefinitionElement> definitionElements, string dictionary)
    {
        var vmResult = new List<Vm.SubArticle>();

        // TODO go recursive if a new nested level is discovered
        foreach (Api.DefinitionElement definitionElement in definitionElements)
        {
            // Top level element is always a Definition (hopefully)
            var definition = (Api.Definition)definitionElement;

            bool childrenAreDefinitions = definition.DefinitionElements.All(de => de is Api.Definition);

            if (childrenAreDefinitions)
            {
                List<Api.Definition> nestedDefinitions = definition.DefinitionElements.Cast<Api.Definition>().ToList();

                foreach (Api.Definition? nestedDefinition in nestedDefinitions)
                {
                    List<Api.DefinitionSubArticle> nestedDefinitionSubArticles = nestedDefinition.DefinitionElements
                        .Where(x => x is Api.DefinitionSubArticle)
                        .Cast<Api.DefinitionSubArticle>()
                        .ToList();

                    IEnumerable<Vm.SubArticle> mappedSubArticles =
                        nestedDefinitionSubArticles.Select(x => MapSubArticle(x, dictionary));

                    vmResult.AddRange(mappedSubArticles);
                }
            }
            else
            {
                List<Api.DefinitionSubArticle> nestedDefinitionSubArticles = definition.DefinitionElements
                    .Where(x => x is Api.DefinitionSubArticle)
                    .Cast<Api.DefinitionSubArticle>()
                    .ToList();

                IEnumerable<Vm.SubArticle> mappedSubArticles =
                    nestedDefinitionSubArticles.Select(x => MapSubArticle(x, dictionary));

                vmResult.AddRange(mappedSubArticles);
            }
        }

        return vmResult;
    }

    public Vm.Definition MapDefinition(List<Api.DefinitionElement> definitionElements, string dictionary)
    {
        var vmResult = new Vm.Definition();

        List<Api.Explanation> explanations = definitionElements
            .Where(de => de is Api.Explanation)
            .Cast<Api.Explanation>()
            .ToList();

        List<Api.Example> examples = definitionElements
            .Where(de => de is Api.Example)
            .Cast<Api.Example>()
            .ToList();

        List<Api.Definition> innerDefinitions = definitionElements
            .Where(de => de is Api.Definition)
            .Cast<Api.Definition>()
            .ToList();

        vmResult.Explanations =
            explanations.Select(x => _contentParser.GetExplanationContent(x, dictionary, true)).ToList();
        vmResult.ExplanationsSimple =
            explanations.Select(x => _contentParser.GetExplanationContent(x, dictionary, false)).ToList();
        vmResult.Examples = examples.Select(x => _contentParser.GetExampleContent(x, dictionary)).ToList();
        vmResult.InnerDefinitions =
            innerDefinitions.Select(x => MapDefinition(x.DefinitionElements, dictionary)).ToList();

        return vmResult;
    }

    public Vm.SubArticle MapSubArticle(Api.DefinitionSubArticle subArticle, string dictionary)
    {
        var vmResult = new Vm.SubArticle();

        if (subArticle.Article?.Body == null)
        {
            return vmResult;
        }

        vmResult.Lemmas = subArticle.Article.Body.Lemmas.Select(MapLemma).ToList();

        vmResult.Definitions = MapDefinitions(subArticle.Article.Body.DefinitionElements, dictionary)
            .ToList();

        return vmResult;
    }

    public List<Vm.Etymology> MapEtymologies(List<Api.EtymologyGroup> etymologyGroups, string dictionary)
    {
        var vmResult = new List<Vm.Etymology>();

        foreach (Api.EtymologyGroup etymologyGroup in etymologyGroups)
        {
            var vmEtymology = new Vm.Etymology();

            switch (etymologyGroup)
            {
                case Api.EtymologyLanguage etymologyLanguage:
                    vmEtymology.Content = _contentParser.GetEtymologyLanguageContent(etymologyLanguage, dictionary);
                    break;
                case Api.EtymologyLitt etymologyLitt:
                    vmEtymology.Content = _contentParser.GetEtymologyLittContent(etymologyLitt, dictionary);
                    break;
                case Api.EtymologyReference etymologyReference:
                    vmEtymology.Content = _contentParser.GetEtymologyReferenceContent(etymologyReference, dictionary);
                    break;
            }

            vmResult.Add(vmEtymology);
        }

        return vmResult;
    }
}

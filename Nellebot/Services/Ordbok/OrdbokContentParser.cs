﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nellebot.Common.Extensions;
using Api = Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Services.Ordbok;

/// <summary>
/// Replaces content which contains $-tokens with values from items array.
/// </summary>
public interface IOrdbokContentParser
{
    string GetEtymologyLanguageContent(Api.EtymologyLanguage etymologyLanguage, string dictionary);

    string GetEtymologyLittContent(Api.EtymologyLitt etymologyLitt, string dictionary);

    string GetEtymologyReferenceContent(Api.EtymologyReference reference, string dictionary);

    string GetExplanationContent(Api.Explanation explanation, string dictionary, bool detailed);

    string GetExampleContent(Api.Example example, string dictionary);
}

public class OrdbokContentParser : IOrdbokContentParser
{
    private readonly ILocalizationService _localizationService;

    public OrdbokContentParser(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string GetEtymologyLanguageContent(Api.EtymologyLanguage etymologyLanguage, string dictionary)
    {
        var contentString = etymologyLanguage.Content;

        var replacementValues = new List<string>();

        foreach (var item in etymologyLanguage.EtymologyLanguageElements)
        {
            switch (item)
            {
                case Api.EtymologyLanguageIdElement idElement:
                    var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.Ordbok, dictionary);
                    replacementValues.Add(localizedIdElement);
                    break;
                case Api.EtymologyLanguageTextElement textElement:
                    replacementValues.Add(textElement.Text);
                    break;
            }
        }

        var finalContentString = ReplaceContentVariables(contentString, replacementValues);

        return finalContentString;
    }

    public string GetEtymologyLittContent(Api.EtymologyLitt etymologyLitt, string dictionary)
    {
        var contentString = etymologyLitt.Content;

        var replacementValues = new List<string>();

        foreach (var item in etymologyLitt.EtymologyLittElements)
        {
            switch (item)
            {
                case Api.EtymologyLittIdElement idElement:
                    var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.Ordbok, dictionary);
                    replacementValues.Add(localizedIdElement);
                    break;
                case Api.EtymologyLittTextElement textElement:
                    replacementValues.Add(textElement.Text);
                    break;
            }
        }

        var finalContentString = ReplaceContentVariables(contentString, replacementValues);

        return finalContentString;
    }

    public string GetEtymologyReferenceContent(Api.EtymologyReference reference, string dictionary)
    {
        var contentString = reference.Content;

        var replacementValues = new List<string>();

        foreach (var item in reference.EtymologyReferenceElements)
        {
            switch (item)
            {
                case Api.EtymologyReferenceIdElement idElement:
                    var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.Ordbok, dictionary);

                    replacementValues.Add(localizedElementId);
                    break;
                case Api.EtymologyReferenceArticleRef articleRef:
                    var firstLemma = articleRef.Lemmas.FirstOrDefault();
                    if (firstLemma != null)
                    {
                        var value = firstLemma.Value;
                        var hgNo = firstLemma.HgNo.ToRomanNumeral();

                        var showHgNo = !string.IsNullOrWhiteSpace(hgNo);

                        var displayValue = showHgNo ? $"{value} ({hgNo})" : value;

                        replacementValues.Add(displayValue);
                    }

                    break;
            }
        }

        var finalContentString = ReplaceContentVariables(contentString, replacementValues);

        return finalContentString;
    }

    public string GetExplanationContent(Api.Explanation explanation, string dictionary, bool detailed)
    {
        var contentString = explanation.Content;

        var replacementValues = new List<string>();

        foreach (var item in explanation.ExplanationItems)
        {
            switch (item)
            {
                case Api.ExplanationIdItem idElement:
                    var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.Ordbok, dictionary);
                    replacementValues.Add(localizedElementId);
                    break;
                case Api.ExplanationTextItem textElement:
                    replacementValues.Add(textElement.Text);
                    break;
                case Api.ExplanationArticleRefItem articleRef:
                    var firstLemma = articleRef.Lemmas.FirstOrDefault();

                    if (firstLemma == null) break;

                    var displayValue = firstLemma.Value;

                    if (detailed)
                    {
                        var hgNo = firstLemma.HgNo.ToRomanNumeral();
                        var definitionOrder = articleRef.DefinitionOrder;

                        var pValues = new List<string>();

                        if (!string.IsNullOrWhiteSpace(hgNo))
                            pValues.Add(hgNo);
                        if (definitionOrder > 0)
                            pValues.Add(definitionOrder.ToString());

                        if (pValues.Count > 0)
                        {
                            displayValue = $"{displayValue} ({string.Join(",", pValues)})";
                        }
                    }

                    replacementValues.Add(displayValue);

                    break;
            }
        }

        var finalContentString = ReplaceContentVariables(contentString, replacementValues);

        return finalContentString;
    }

    public string GetExampleContent(Api.Example example, string dictionary)
    {
        var contentString = example.Quote.Content;

        var replacementValues = new List<string>();

        foreach (var item in example.Quote.QuoteItems)
        {
            switch (item)
            {
                case Api.QuoteIdItem idElement:
                    var localizedIdItem = _localizationService.GetString(idElement.Id, LocalizationResource.Ordbok, dictionary);
                    replacementValues.Add(localizedIdItem);
                    break;
                case Api.QuoteTextItem textElement:
                    replacementValues.Add(textElement.Text);
                    break;
            }
        }

        var finalContentString = ReplaceContentVariables(contentString, replacementValues);

        return finalContentString;
    }

    private static string ReplaceContentVariables(string contentString, List<string> values)
    {
        var contentHasVariables = values.Any();

        if (!contentHasVariables)
            return contentString;

        var regex = new Regex(Regex.Escape("$"));

        values.ForEach(v => contentString = regex.Replace(contentString, v, 1));

        return contentString;
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Services.Ordbok;

namespace Nellebot.CommandModules;

public abstract class OrdbokSuggestProvider
{
    private readonly OrdbokHttpClient _ordbokHttpClient;

    protected OrdbokSuggestProvider(OrdbokHttpClient ordbokHttpClient)
    {
        _ordbokHttpClient = ordbokHttpClient;
    }

    protected async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> GetDiscordAutoCompleteChoices(
        string dict,
        string? query)
    {
        if (string.IsNullOrEmpty(query))
            return [];

        OrdbokSuggestResponse apiResults = await _ordbokHttpClient.Suggest(
            dict,
            query,
            maxResults: 10,
            CancellationToken.None);

        List<(string Key, Tupleish Value)> flattenedResults =
            apiResults.SuggestionResults.SelectMany(x => x.Value.Select(v => (x.Key, v))).ToList();

        List<DiscordAutoCompleteChoice> choiceResults =
            flattenedResults.Select(x =>
            {
                string value = x.Value.Item1;
                string displayValue = x.Key == "exact" ? value : $"{value} [{x.Key}]";
                return new DiscordAutoCompleteChoice(displayValue, value);
            }).ToList();

        return choiceResults;
    }
}

public class OrdbokBmSuggestProvider : OrdbokSuggestProvider,
    IAutoCompleteProvider
{
    public OrdbokBmSuggestProvider(OrdbokHttpClient ordbokHttpClient)
        : base(ordbokHttpClient)
    {
    }

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        return await GetDiscordAutoCompleteChoices(OrdbokDictionaryMap.Bokmal, context.UserInput);
    }
}

public class OrdbokNnSuggestProvider : OrdbokSuggestProvider,
    IAutoCompleteProvider
{
    public OrdbokNnSuggestProvider(OrdbokHttpClient ordbokHttpClient)
        : base(ordbokHttpClient)
    {
    }

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        return await GetDiscordAutoCompleteChoices(OrdbokDictionaryMap.Nynorsk, context.UserInput);
    }
}

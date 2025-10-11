using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class OrdbokModule
{
    private readonly RequestQueueChannel _requestQueue;

    public OrdbokModule(RequestQueueChannel commandQueue)
    {
        _requestQueue = commandQueue;
    }

    [BaseCommandCheck]
    [Command("bm")]
    [Description("Search Bokmål dictionary")]
    public Task OrbokSearchBokmal(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")] [SlashAutoCompleteProvider<OrdbokBmSuggestProvider>]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
            IsAutoComplete = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("nn")]
    [Description("Search Nynorsk dictionary")]
    public Task OrdbokSearchNynorsk(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")] [SlashAutoCompleteProvider<OrdbokNnSuggestProvider>]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
            IsAutoComplete = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("bm-free-text")]
    [Description("Search Bokmål dictionary (free text)")]
    public Task OrdbokSearchBokmalFreeText(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("nn-free-text")]
    [Description("Search Nynorsk dictionary (free text)")]
    public Task OrdbokSearchNynorskFreeText(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }
}

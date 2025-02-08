using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class OrdbokSlashModule
{
    private readonly RequestQueueChannel _requestQueue;

    public OrdbokSlashModule(RequestQueueChannel commandQueue)
    {
        _requestQueue = commandQueue;
    }

    [BaseCommandCheck]
    [Command("bm")]
    [Description("Search Bokmål dictionary")]
    public Task OrdbokSearchBokmal(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQueryV2(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("nn")]
    [Description("Search Nynorsk dictionary")]
    public Task OrdbokSearchNynorsk(
        SlashCommandContext ctx,
        [Parameter("query")] [Description("What to search for")]
        string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQueryV2(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }
}

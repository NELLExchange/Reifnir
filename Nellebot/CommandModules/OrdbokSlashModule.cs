using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class OrdbokSlashModule : ApplicationCommandModule
{
    private readonly RequestQueueChannel _requestQueue;

    public OrdbokSlashModule(RequestQueueChannel commandQueue)
    {
        _requestQueue = commandQueue;
    }

    [SlashCommand("bm", "Search Bokmål dictionary")]
    public Task OrdbokSearchBokmal(InteractionContext ctx, [Option("query", "What to search for")] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [SlashCommand("nn", "Search Nynorsk dictionary")]
    public Task OrdbokSearchNynorsk(InteractionContext ctx, [Option("query", "What to search for")] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }
}

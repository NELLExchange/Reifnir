using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class OrdbokLegacyLegacy
{
    private readonly RequestQueueChannel _requestQueue;

    public OrdbokLegacyLegacy(RequestQueueChannel commandQueue)
    {
        _requestQueue = commandQueue;
    }

    [BaseCommandCheck]
    [Command("bm-legacy")]
    ////[Aliases("nb")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public Task OrdbokSearchBokmal(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("nn-legacy")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public Task OrdbokSearchNynorsk(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("bm-t-legacy")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public Task OrdbokSearchBokmalDebugTemplate(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
            AttachTemplate = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("nn-t-legacy")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public Task OrdbokSearchNynorskDebugTemplate(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
            AttachTemplate = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [BaseCommandCheck]
    [Command("gibb")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public Task Gibb(CommandContext ctx)
    {
        var gibbCommand = new GibbCommand(ctx);

        return _requestQueue.Writer.WriteAsync(gibbCommand).AsTask();
    }
}

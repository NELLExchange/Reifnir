using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Common.Models.Ordbok.ViewModels;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Scriban;
using Api = Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.CommandHandlers.Ordbok;

public record SearchOrdbokQuery : BotSlashCommand
{
    public SearchOrdbokQuery(SlashCommandContext ctx)
        : base(ctx)
    {
    }

    public string Query { get; init; } = string.Empty;

    public string Dictionary { get; init; } = string.Empty;

    public bool IsAutoComplete { get; init; }
}

public class SearchOrdbokHandler : IRequestHandler<SearchOrdbokQuery>
{
    private const int MaxArticlesPerPage = 5;
    private const int MaxDefinitionsInTextForm = 5;
    private const string CopyrightText = "Universitetet i Bergen og Språkrådet - ordbokene.no";

    private readonly OrdbokHttpClient _ordbokClient;
    private readonly OrdbokModelMapper _ordbokModelMapper;
    private readonly ScribanTemplateLoader _templateLoader;

    public SearchOrdbokHandler(
        OrdbokHttpClient ordbokClient,
        OrdbokModelMapper ordbokModelMapper,
        ScribanTemplateLoader templateLoader)
    {
        _ordbokClient = ordbokClient;
        _ordbokModelMapper = ordbokModelMapper;
        _templateLoader = templateLoader;
    }

    public async Task Handle(SearchOrdbokQuery request, CancellationToken cancellationToken)
    {
        SlashCommandContext ctx = request.Ctx;
        string query = request.Query;
        string dictionary = request.Dictionary;
        bool isAutoComplete = request.IsAutoComplete;
        DiscordUser user = ctx.User;

        await ctx.DeferResponseAsync();

        var isExactSearch = false;

        if (isAutoComplete)
        {
            Api.OrdbokSuggestResponse autoCompleteResult = await _ordbokClient.Suggest(
                request.Dictionary,
                query,
                maxResults: 10,
                cancellationToken);

            isExactSearch = autoCompleteResult.SuggestionResults
                .SelectMany(x => x.Value)
                .Select(x => x.Item1)
                .Contains(query, StringComparer.InvariantCultureIgnoreCase);
        }

        Api.OrdbokSearchResponse? searchResponse = await _ordbokClient.Search(
            request.Dictionary,
            query,
            isExactSearch,
            cancellationToken);

        int[]? articleIds = searchResponse?.Articles[dictionary];

        if (articleIds == null || articleIds.Length == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No match"));
            return;
        }

        List<Api.Article?> ordbokArticles =
            await _ordbokClient.GetArticles(dictionary, articleIds, cancellationToken);

        List<Article> articles = MapAndSelectArticles(ordbokArticles, dictionary);

        var title =
            $"{(dictionary == OrdbokDictionaryMap.Bokmal ? "Bokmålsordboka" : "Nynorskordboka")} | {articles.Count} treff";

        var queryUrl =
            $"https://ordbokene.no/{(dictionary == OrdbokDictionaryMap.Bokmal ? "bm" : "nn")}/search?q={query}&scope=ei";

        IEnumerable<Page> messagePages = await BuildPages(articles, title, queryUrl);

        await ctx.Interaction.SendPaginatedResponseAsync(
            ephemeral: false,
            user,
            messagePages,
            token: cancellationToken);
    }

    private async Task<IEnumerable<Page>> BuildPages(List<Article> articles, string title, string queryUrl)
    {
        var pages = new List<Page>();

        int articleCount = articles.Count;
        var pageCount = (int)Math.Ceiling((double)articleCount / MaxArticlesPerPage);

        // var z = new UriBuilder(queryUrl);
        // z.Query = HttpUtility.UrlEncode(z.Query);
        // Uri parsedUrl = z.Uri;

        // var parsedUrl = new Uri(HttpUtility.UrlEncode(queryUrl));
        string encodedUrl = EmbedBuilderHelper.EncodeUrlForDiscordEmbed(queryUrl);

        for (var i = 0; i < pageCount; i++)
        {
            int offset = i * MaxArticlesPerPage;

            List<Article> articlesOnPage = articles.Skip(offset).Take(MaxArticlesPerPage).ToList();

            var pagination = new PaginationArgs(offset + 1, i + 1, pageCount);

            string renderedTemplate = await RenderTextTemplate(articlesOnPage, pagination);

            string truncatedContent =
                renderedTemplate[..Math.Min(renderedTemplate.Length, DiscordConstants.MaxEmbedContentLength)];

            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithUrl(encodedUrl)
                .WithDescription(truncatedContent)
                .WithFooter(CopyrightText)
                .WithColor(DiscordConstants.DefaultEmbedColor);

            pages.Add(new Page(embed: eb));
        }

        return pages;
    }

    private async Task<string> RenderTextTemplate(List<Article> articles, PaginationArgs pagination)
    {
        Template textTemplate = await _templateLoader.LoadTemplate("OrdbokArticle");

        string? textTemplateResult =
            await textTemplate.RenderAsync(new { articles, pagination, maxDefinitions = MaxDefinitionsInTextForm });

        return textTemplateResult;
    }

    private List<Article> MapAndSelectArticles(List<Api.Article?> ordbokArticles, string dictionary)
    {
        List<Article> articles = ordbokArticles
            .Where(a => a != null)
            .Select(x => _ordbokModelMapper.MapArticle(x!, dictionary))
            .OrderBy(a => a.Lemmas.Max(l => l.HgNo))
            .ToList();

        return articles;
    }
}

internal record PaginationArgs(int PageOffset, int CurrentPage, int PageCount);

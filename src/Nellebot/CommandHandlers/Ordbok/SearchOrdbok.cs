using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Services;
using Nellebot.Services.HtmlToImage;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Scriban;
using Api = Nellebot.Common.Models.Ordbok.Api;
using Vm = Nellebot.Common.Models.Ordbok.ViewModels;

namespace Nellebot.CommandHandlers.Ordbok;

public record SearchOrdbokQuery : BotCommandQuery
{
    public SearchOrdbokQuery(CommandContext ctx)
        : base(ctx)
    { }

    public string Query { get; init; } = string.Empty;

    public string Dictionary { get; init; } = string.Empty;

    public bool AttachTemplate { get; init; }
}

public class SearchOrdbokHandler : IRequestHandler<SearchOrdbokQuery>
{
    private const int MaxDefinitionsInTextForm = 5;
    private readonly HtmlToImageService _htmlToImageService;
    private readonly ILogger<SearchOrdbokHandler> _logger;

    private readonly OrdbokHttpClient _ordbokClient;
    private readonly OrdbokModelMapper _ordbokModelMapper;
    private readonly ScribanTemplateLoader _templateLoader;

    public SearchOrdbokHandler(
        OrdbokHttpClient ordbokClient,
        OrdbokModelMapper ordbokModelMapper,
        ScribanTemplateLoader templateLoader,
        HtmlToImageService htmlToImageService,
        ILogger<SearchOrdbokHandler> logger)
    {
        _ordbokClient = ordbokClient;
        _ordbokModelMapper = ordbokModelMapper;
        _templateLoader = templateLoader;
        _htmlToImageService = htmlToImageService;
        _logger = logger;
    }

    public async Task Handle(SearchOrdbokQuery request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string query = request.Query;
        string dictionary = request.Dictionary;
        bool attachTemplate = request.AttachTemplate;

        Api.OrdbokSearchResponse? searchResponse = await _ordbokClient.Search(
            request.Dictionary,
            query,
            cancellationToken);

        int[]? articleIds = searchResponse?.Articles[dictionary];

        if (articleIds == null || articleIds.Length == 0)
        {
            await ctx.RespondAsync("No match");
            return;
        }

        List<Api.Article?> ordbokArticles =
            await _ordbokClient.GetArticles(dictionary, articleIds.ToList(), cancellationToken);

        List<Vm.Article> articles = MapAndSelectArticles(ordbokArticles, dictionary);

        var queryUrl = $"https://ordbokene.no/{(dictionary == OrdbokDictionaryMap.Bokmal ? "bm" : "nn")}/w/{query}";

        string textTemplateResult = await RenderTextTemplate(articles);

        string htmlTemplateResult = await RenderHtmlTemplate(dictionary, articles);

        string truncatedContent = textTemplateResult.Substring(
            0,
            Math.Min(
                textTemplateResult.Length,
                DiscordConstants.MaxEmbedContentLength));

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
            .WithTitle(dictionary == OrdbokDictionaryMap.Bokmal ? "Bokmålsordboka" : "Nynorskordboka")
            .WithUrl(queryUrl)
            .WithDescription(truncatedContent)
            .WithFooter("Universitetet i Bergen og Språkrådet - ordbokene.no")
            .WithColor(DiscordConstants.DefaultEmbedColor);

        var mb = new DiscordMessageBuilder();

        FileStream? imageFileStream = null;
        FileStream? htmlFileStream = null;

        try
        {
            GenerateImageFileResult result = await _htmlToImageService.GenerateImageFile(htmlTemplateResult);

            imageFileStream = result.ImageFileStream;
            htmlFileStream = result.HtmlFileStream;

            if (!attachTemplate)
            {
                eb = eb.WithImageUrl($"attachment://{result.ImageFileName}");
                mb = mb.AddFile(result.ImageFileName, result.ImageFileStream);
            }
            else
            {
                mb = mb.AddFile(result.HtmlFileStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(SearchOrdbokQuery));
        }

        mb = mb.AddEmbed(eb.Build());

        await ctx.RespondAsync(mb);

        if (imageFileStream != null)
        {
            await imageFileStream.DisposeAsync();
        }

        if (htmlFileStream != null)
        {
            await htmlFileStream.DisposeAsync();
        }
    }

    private async Task<string> RenderTextTemplate(List<Vm.Article> articles)
    {
        string textTemplateSource = await _templateLoader.LoadTemplate("OrdbokArticle", ScribanTemplateType.Text);
        Template? textTemplate = Template.Parse(textTemplateSource);

        int maxDefinitions = MaxDefinitionsInTextForm;

        string? textTemplateResult = textTemplate.Render(new { articles, maxDefinitions });

        return textTemplateResult;
    }

    private async Task<string> RenderHtmlTemplate(string dictionary, List<Vm.Article> articles)
    {
        string htmlTemplateSource = await _templateLoader.LoadTemplate("OrdbokArticle", ScribanTemplateType.Html);
        Template? htmlTemplate = Template.Parse(htmlTemplateSource);

        string? htmlTemplateResult = htmlTemplate.Render(new { articles, dictionary });

        return htmlTemplateResult;
    }

    private List<Vm.Article> MapAndSelectArticles(List<Api.Article?> ordbokArticles, string dictionary)
    {
        List<Vm.Article> articles = ordbokArticles
            .Where(a => a != null)
            .Select(x => _ordbokModelMapper.MapArticle(x!, dictionary))
            .OrderBy(a => a.Lemmas.Max(l => l.HgNo))
            .ToList();

        return articles;
    }
}

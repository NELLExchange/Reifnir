using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Services.Ordbok;

public class OrdbokHttpClient
{
    private const int MaxArticles = 50;

    private readonly HttpClient _client;

    public OrdbokHttpClient(HttpClient client)
    {
        _client = client;

        _client.BaseAddress = new Uri("https://ord.uib.no/");
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<OrdbokSearchResponse> Search(
        string dictionary,
        string searchText,
        bool exact,
        CancellationToken cancellationToken = default)
    {
        const string scope = "ei";
        if (!exact && !searchText.Contains('*') && !searchText.Contains('%'))
        {
            searchText = $"*{searchText}*";
        }

        var requestUri = $"api/articles?w={searchText}&dict={dictionary}&scope={scope}";

        HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var searchResponse =
            await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(
                jsonStream,
                options: null,
                cancellationToken);

        return searchResponse ?? throw new InvalidOperationException("Unable to deserialize response");
    }

    public async Task<OrdbokSearchResponse> GetAll(
        string dictionary,
        string wordClass,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/articles?w=*&wc={wordClass}&dict={dictionary}&scope=f";

        HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var searchResponse =
            await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(jsonStream, options: null, cancellationToken);

        return searchResponse ?? throw new InvalidOperationException("Unable to deserialize response");
    }

    public async Task<OrdbokSuggestResponse> Suggest(
        string dictionary,
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/suggest?q={query}&dict={dictionary}&n={maxResults}&include=ei";

        HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var searchResponse =
            await JsonSerializer.DeserializeAsync<OrdbokSuggestResponse>(jsonStream, options: null, cancellationToken);

        return searchResponse ?? throw new InvalidOperationException("Unable to deserialize response");
    }

    public async Task<Article?> GetArticle(
        string dictionary,
        int articleId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"{dictionary}/article/{articleId}.json";

        HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var article = await JsonSerializer.DeserializeAsync<Article>(jsonStream, options: null, cancellationToken);

        return article;
    }

    public async Task<List<Article>> GetArticles(
        string dictionary,
        int[] articleIds,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<Article?>> tasks = articleIds.Take(MaxArticles)
            .Select(id => GetArticle(dictionary, id, cancellationToken));

        Article?[] result = await Task.WhenAll(tasks);

        List<Article> nonNullArticles = result.Where(a => a != null).Select(a => a!).ToList();

        return nonNullArticles;
    }

    public async Task<OrdbokConcepts?> GetConcepts(string dictionary, CancellationToken cancellationToken = default)
    {
        var requestUri = $"{dictionary}/concepts.json";

        HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var article =
            await JsonSerializer.DeserializeAsync<OrdbokConcepts>(jsonStream, options: null, cancellationToken);

        return article;
    }
}

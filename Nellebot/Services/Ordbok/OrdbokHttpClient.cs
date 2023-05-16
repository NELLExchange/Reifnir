﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Services.Ordbok
{
    public class OrdbokHttpClient
    {
        private const int MaxArticles = 5;

        // TODO use HttpClientFactory
        private readonly HttpClient _client;

        public OrdbokHttpClient(HttpClient client)
        {
            _client = client;

            _client.BaseAddress = new Uri("https://ord.uib.no/");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<OrdbokSearchResponse?> Search(string dictionary, string query, CancellationToken cancellationToken = default)
        {
            var requestUri = $"api/articles?w={query}&dict={dictionary}&scope=ei";

            var response = await _client.GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var searchResponse = await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(jsonStream, options: null, cancellationToken);

            return searchResponse;
        }

        public async Task<OrdbokSearchResponse?> GetAll(string dictionary, string wordClass, CancellationToken cancellationToken = default)
        {
            var requestUri = $"api/articles?w=*&wc={wordClass}&dict={dictionary}&scope=f";

            var response = await _client.GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var searchResponse = await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(jsonStream, options: null, cancellationToken);

            return searchResponse;
        }

        public async Task<Article?> GetArticle(string dictionary, int articleId, CancellationToken cancellationToken = default)
        {
            var requestUri = $"{dictionary}/article/{articleId}.json";

            var response = await _client.GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var article = await JsonSerializer.DeserializeAsync<Article>(jsonStream, options: null, cancellationToken);

            return article;
        }

        public async Task<List<Article?>> GetArticles(string dictionary, List<int> articleIds, CancellationToken cancellationToken = default)
        {
            var tasks = articleIds.Take(MaxArticles).Select(id => GetArticle(dictionary, id));

            var result = await Task.WhenAll(tasks);

            if (result == null)
                return Enumerable.Empty<Article?>().ToList();

            return result.ToList();
        }

        public async Task<OrdbokConcepts?> GetConcepts(string dictionary, CancellationToken cancellationToken = default)
        {
            var requestUri = $"{dictionary}/concepts.json";

            var response = await _client.GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var article = await JsonSerializer.DeserializeAsync<OrdbokConcepts>(jsonStream, options: null, cancellationToken);

            return article;
        }
    }
}

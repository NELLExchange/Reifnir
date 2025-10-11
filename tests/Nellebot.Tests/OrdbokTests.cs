using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using NSubstitute;
using ApiArticle = Nellebot.Common.Models.Ordbok.Api.Article;
using VmArticle = Nellebot.Common.Models.Ordbok.ViewModels.Article;

namespace Nellebot.Tests;

[TestClass]
public class OrdbokTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task TestArticleDeserialization()
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;

        string file = Path.Combine(directory, "TestFiles/test_hus_24627.json");

        string json = await File.ReadAllTextAsync(file, TestContext.CancellationToken);

        try
        {
            var result = JsonSerializer.Deserialize<ApiArticle>(json);

            var localizationService = Substitute.For<ILocalizationService>();

            localizationService
                .GetString(Arg.Any<string>(), Arg.Any<LocalizationResource>(), Arg.Any<string>())
                .Returns(x => x[0]);

            var ordbokContentParser = new OrdbokContentParser(localizationService);

            var modelMapper = new OrdbokModelMapper(ordbokContentParser, localizationService);

            VmArticle article = modelMapper.MapArticle(result!, "bm");

            Assert.IsNotNull(article);
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.ToString());
        }
    }

    [TestMethod]
    public async Task TestSuggestResponseDeserialization()
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;

        string file = Path.Combine(directory, "TestFiles/suggest_response_hus.json");

        string json = await File.ReadAllTextAsync(file, TestContext.CancellationToken);

        try
        {
            var result = JsonSerializer.Deserialize<OrdbokSuggestResponse>(json);

            Assert.IsNotNull(result);
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.ToString());
        }
    }

    [TestMethod]
    public async Task TestSuggestApi()
    {
        var httpClient = new HttpClient();
        var ordbokHttpClient = new OrdbokHttpClient(httpClient);

        OrdbokSuggestResponse result = await ordbokHttpClient.Suggest(
            "bm",
            "hus",
            maxResults: 10,
            TestContext.CancellationToken);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TestDiscordEmbedUrlEncoding()
    {
        const string queryUrl = "https://site.example.com?q=query with spaces";
        const string expectedUrl = "https://site.example.com?q=query%20with%20spaces";

        string actualUrl = EmbedBuilderHelper.EncodeUrlForDiscordEmbed(queryUrl);

        Assert.AreEqual(expectedUrl, actualUrl);
    }
}

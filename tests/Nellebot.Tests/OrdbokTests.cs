using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.Models.Ordbok.ViewModels;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using NSubstitute;
using OrdbokApi = Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Tests;

[TestClass]
[Ignore]
public class OrdbokTests
{
    [TestMethod]
    public async Task TestArticleDeserialization()
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;

        string file = Path.Combine(directory, "TestFiles/test.json");

        string json = await File.ReadAllTextAsync(file);

        try
        {
            var result = JsonSerializer.Deserialize<OrdbokApi.Article>(json);

            var localizationService = Substitute.For<ILocalizationService>();

            localizationService
                .GetString(Arg.Any<string>(), Arg.Any<LocalizationResource>(), Arg.Any<string>())
                .Returns(x => x[0]);

            var ordbokContentParser = new OrdbokContentParser(localizationService);

            var modelMapper = new OrdbokModelMapper(ordbokContentParser, localizationService);

            Article article = modelMapper.MapArticle(result!, "bm");
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.ToString());
        }
    }
}

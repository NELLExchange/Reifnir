using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models.Ordbok.Store;

namespace Nellebot.Data.Repositories;

public class OrdbokRepository
{
    private readonly BotDbContext _dbContext;

    public OrdbokRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrdbokArticleStore?> GetArticleStore(
        string dictionary,
        string wordClass,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrdbokArticlesStore.FindAsync(new[] { dictionary, wordClass }, cancellationToken);
    }

    public async Task SaveArticleStore(
        OrdbokArticleStore ordbokArticleStore,
        CancellationToken cancellationToken = default)
    {
        OrdbokArticleStore? existingArticleStore = await _dbContext.OrdbokArticlesStore.FindAsync(
            new[] { ordbokArticleStore.Dictionary, ordbokArticleStore.WordClass },
            cancellationToken);

        if (existingArticleStore == null)
        {
            _dbContext.Add(ordbokArticleStore);
        }
        else
        {
            existingArticleStore.ArticleCount = ordbokArticleStore.ArticleCount;
            existingArticleStore.ArticleList = ordbokArticleStore.ArticleList;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetArticleCount(
        string dictionary,
        string wordClass,
        CancellationToken cancellationToken = default)
    {
        int count = await _dbContext.OrdbokArticlesStore
            .Where(x => x.Dictionary == dictionary && x.WordClass == wordClass)
            .Select(x => x.ArticleCount)
            .SingleOrDefaultAsync(cancellationToken);

        return count;
    }

    public async Task<int> GetArticleIdAtIndex(
        string dictionary,
        string wordClass,
        int index,
        CancellationToken cancellationToken = default)
    {
        int articleId = await _dbContext.OrdbokArticlesStore
            .Where(x => x.Dictionary == dictionary && x.WordClass == wordClass)
            .Select(x => x.ArticleList[index])
            .SingleOrDefaultAsync(cancellationToken);

        return articleId;
    }

    public async Task<OrdbokConceptStore?> GetConceptStore(
        string dictionary,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrdbokConceptStore.FindAsync(new[] { dictionary }, cancellationToken);
    }

    public async Task SaveConceptStore(OrdbokConceptStore ordbokConcepts, CancellationToken cancellationToken = default)
    {
        OrdbokConceptStore? existingConceptStore = await _dbContext.OrdbokConceptStore.FindAsync(
            new[] { ordbokConcepts.Dictionary },
            cancellationToken);

        if (existingConceptStore == null)
        {
            _dbContext.Add(ordbokConcepts);
        }
        else
        {
            existingConceptStore.Concepts = ordbokConcepts.Concepts;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

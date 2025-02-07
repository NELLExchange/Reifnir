using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Nellebot.Infrastructure;

public class SharedCache
{
    private readonly MemoryCache _cache;

    public SharedCache()
    {
        _cache = new MemoryCache(
            new MemoryCacheOptions
            {
                SizeLimit = 1024,
            });
    }

    public async Task<T?> LoadFromCacheAsync<T>(string key, Func<Task<T>> delegateFunction, TimeSpan duration)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            return value;
        }

        T loadedData = await delegateFunction();

        _cache.Set(
            key,
            loadedData,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration,
                Size = 1,
            });

        return loadedData;
    }

    public void FlushCache(string key)
    {
        _cache.Remove(key);
    }
}

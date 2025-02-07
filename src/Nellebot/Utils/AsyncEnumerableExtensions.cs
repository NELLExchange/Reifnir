using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nellebot.Utils;

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (T item in source)
        {
            list.Add(item);
        }

        return list;
    }

    public static async Task<int> CountAsync<T>(this IAsyncEnumerable<T> source)
    {
        var count = 0;
        await foreach (T _ in source)
        {
            count++;
        }

        return count;
    }
}

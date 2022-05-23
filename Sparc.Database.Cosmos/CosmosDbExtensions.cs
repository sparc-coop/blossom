using Microsoft.Azure.Cosmos.Linq;

namespace Sparc.Database.Cosmos;

public static class CosmosDbExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
    {
        var iterator = query.ToFeedIterator();
        var result = await iterator.ReadNextAsync();
        return result.ToList();
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> query)
    {
        var iterator = query.ToFeedIterator();

        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync())
            {
                yield return item;
            }
        }
    }
}

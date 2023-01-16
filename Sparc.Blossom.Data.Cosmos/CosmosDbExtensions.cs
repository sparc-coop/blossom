using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public static class CosmosDbExtensions
{
    public static IQueryable<T> WithPartitionKey<T>(this IQueryable<T> query, string partitionKey) where T : class
    {
        return CosmosQueryableExtensions.WithPartitionKey(query, partitionKey);
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

    public static IQueryable<T> Query<T>(this IRepository<T> repository, string? partitionKey) where T : class, IRoot<string>
    {
        if (repository is CosmosDbRepository<T> cosmosRepository && partitionKey != null)
            return cosmosRepository.PartitionQuery(partitionKey);

        return repository.Query;
    }
}

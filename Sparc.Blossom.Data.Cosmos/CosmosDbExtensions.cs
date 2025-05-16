using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public static class CosmosDbExtensions
{
    public static IQueryable<T> WithPartitionKey<T>(this IQueryable<T> query, string partitionKey) where T : class
    {
        var hierarchicalPartitionKey = new { TenantId = "sparc", UserId = "sparc-admin", DatabaseId = partitionKey };
        return CosmosQueryableExtensions.WithPartitionKey(query, hierarchicalPartitionKey);
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

    public static async Task<List<T>> ToCosmosAsync<T>(this IQueryable<T> query)
    {
        var iterator = query.ToFeedIterator();

        var results = new List<T>();
        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync())
            {
                results.Add(item);
            }
        }
        return results;
    }

    public static async Task<T?> CosmosFirstOrDefaultAsync<T>(this IQueryable<T> query)
    {
        var results = await query.ToCosmosAsync();
        return results.FirstOrDefault();
    }

    public static IQueryable<T> Query<T>(this IRepository<T> repository, string? partitionKey) where T : BlossomEntity<string>
    {
        if (repository is CosmosDbRepository<T> cosmosRepository && partitionKey != null)
            return cosmosRepository.PartitionQuery(partitionKey);

        if (repository is CosmosDbSimpleRepository<T> cosmosSimpleRepository && partitionKey != null)
            return cosmosSimpleRepository.PartitionQuery(partitionKey);

        return repository.Query;
    }

    public static async Task<List<U>> GetAllAsync<T, U>(this IRepository<T> repository, string? partitionKey, string sql, params object[] parameters) 
        where T : BlossomEntity<string>
    {
        if (repository is CosmosDbRepository<T> cosmosRepository)
            return await cosmosRepository.FromSqlAsync<U>(sql, partitionKey, null, parameters);

        return repository.FromSqlRaw(sql, parameters).Cast<U>().ToList();
    }
}

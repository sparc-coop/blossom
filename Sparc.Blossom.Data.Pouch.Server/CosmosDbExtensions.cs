using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Sparc.Blossom.Data.Pouch.Server
{
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

        public static async Task<List<dynamic>> FromSqlAsync(this Container container, string partitionKey, string sql)
        {
            var options = new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };
            var iterator = container.GetItemQueryIterator<dynamic>(sql, requestOptions: options);
            var results = new List<dynamic>();
            while (iterator.HasMoreResults)
            {
                FeedResponse<dynamic> response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}

using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

public record VectorSearchResult<T>(T Item, double Score);
public static class BlossomVectorExtensions
{
    public static async Task<List<VectorSearchResult<T>>> SearchAsync<T>(this IRepository<T> repository, string spaceId, BlossomVector vector, int count, double? similarityThreshold = null)
        where T : BlossomEntity<string>, IVectorizable
    {
        var top = similarityThreshold < 0 ? 10000 : count;
        var similarityThresholdStatement = similarityThreshold.HasValue
            ? similarityThreshold.Value >= 0
                 ? $"AND VectorDistance(c.Vector, {vector}) >= {similarityThreshold.Value}"
                 : $"AND VectorDistance(c.Vector, {vector}) <= {similarityThreshold.Value}"
             : string.Empty;

        var query = $@"
            SELECT TOP {top} c as Item, VectorDistance(c.Vector, {vector}) as Score
            FROM c
            WHERE c.SpaceId = '{spaceId}' AND c.EntityType = '{typeof(T).Name}'
            {similarityThresholdStatement}
            ORDER BY VectorDistance(c.Vector, {vector})";

        var cosmosVectors = repository as CosmosDbSimpleRepository<T>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<VectorSearchResult<T>>(query, spaceId);

        if (similarityThreshold < 0)
            similarVectorsInSpace = similarVectorsInSpace.TakeLast(count).ToList();

        return similarVectorsInSpace;
    }
}
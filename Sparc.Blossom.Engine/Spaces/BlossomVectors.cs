using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

public static class BlossomVectorExtensions
{
    public static async Task<List<BlossomScoredVector<T>>> SearchAsync<T>(this IRepository<T> repository, string spaceId, BlossomVector vector, int count)
        where T : BlossomEntity<string>, IVectorizable
    {
        var query = $@"
            SELECT TOP {count} *
            FROM c
            WHERE c.SpaceId = '{spaceId}' AND c._type = '{typeof(T).Name}'
            ORDER BY VectorDistance(c.Vector, {vector})";

        var cosmosVectors = repository as CosmosDbSimpleRepository<T>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<T>(query, spaceId);

        var result = similarVectorsInSpace.Select(item => new BlossomScoredVector<T>(item, item.Vector.SimilarityTo(vector))).ToList();
        return result;
    }
}
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

public class BlossomVectors(
    IRepository<BlossomVector> vectors, 
    IRepository<BlossomPost> posts,
    IEnumerable<ITranslator> translators)
{
    public async Task<BlossomVector?> FindAsync(string spaceId, string id)
        => await vectors.FindAsync(spaceId, id);

    
    public async Task<List<BlossomVector>> GetAsync(string spaceId, string type, decimal sampleSize)
    {
        var query = vectors.Query.Where(x => x.SpaceId == spaceId && x.Type == "Post");
        int take = (int)(query.Count() * sampleSize);

        return await query
            .OrderBy(x => x.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task UpdateAsync(BlossomVector vector) => await vectors.UpdateAsync(vector);
    public async Task UpdateAsync(IEnumerable<BlossomVector> blossomVectors) => await vectors.UpdateAsync(blossomVectors);

    public async Task<List<BlossomVector>> SearchAsync(string parentSpaceId, BlossomSpace space, string type, int count, bool furthestAway = false)
    {
        var spaceVector = await vectors.Query
            .Where(x => x.SpaceId == parentSpaceId && x.Id == space.Id)
            .Select(x => x.Vector)
            .FirstOrDefaultAsync()
            ?? throw new Exception("Space vector not found");

        var top = furthestAway ? 10000 : count;
        var query = $@"
            SELECT TOP {top} c.id, c.Type, c.Text, c.TargetUrl
            FROM c
            WHERE c.SpaceId = '{parentSpaceId}' AND c.Type = '{type}'
            ORDER BY VectorDistance(c.Vector, {new BlossomVector(spaceVector)})";

        var cosmosVectors = vectors as CosmosDbSimpleRepository<BlossomVector>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<BlossomVector>(query, parentSpaceId);
        if (furthestAway)
            similarVectorsInSpace = similarVectorsInSpace.TakeLast(count).ToList();

        return similarVectorsInSpace;
    }

    internal async Task IndexAsync(string spaceId, int lastX, int lookback)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId).ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);

        var messages = await posts.Query.Where(x => x.Domain == BlossomSpaces.Domain && x.SpaceId == spaceId)
            .OrderByDescending(x => x.Timestamp)
            .Take(lastX + lookback)
            .ToListAsync();

        var offset = 0;
        var batchSize = 1000;

        do
        {
            var batch = messages
                        .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                        //.OrderBy(x => x.Sequence)
                        .Skip(offset)
                        .Take(batchSize)
                        .ToList();

            var ids = batch.Select(x => x.Id).ToList();
            //var existing = await vectorRepo.Query.Where(x => ids.Contains(x.TargetUrl)).Select(x => x.TargetUrl).ToListAsync();
            //batch = batch.Where(x => !existing.Contains(x.Id)).ToList();
            if (batch.Count > 0)
            {
                var translator = translators.OfType<OpenAITranslator>().First();
                var newVectors = await translator.VectorizeAsync(batch, lastX, lookback);
                await vectors.AddAsync(newVectors);
            }
            offset += batchSize;
        } while (offset < messages.Count);
    }

    

    internal async Task AddAsync(BlossomPost post)
    {
        var translator = translators.OfType<OpenAITranslator>().First();
        var vector = await translator.VectorizeAsync(post);
        await vectors.AddAsync(vector);
    }

    internal async Task ClearAsync(string spaceId)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId && x.Type != "Post").ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);
    }
}

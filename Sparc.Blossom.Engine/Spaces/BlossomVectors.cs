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

    public async Task<BlossomVector?> FindAsync(BlossomSpace space) =>
        await FindAsync(space.Domain, space.Id);
    
    public async Task<List<BlossomPostWithVector>> GetAsync(IEnumerable<BlossomPost> posts)
    {
        if (!posts.Any())
            return [];
        
        var spaceId = posts.First().SpaceId;
        var postIds = posts.Select(x => x.Id).ToList();

        var vectorsInPosts = await vectors.Query
            .Where(x => x.SpaceId == spaceId && postIds.Contains(x.Id))
            .ToListAsync();

        var result = from post in posts
                     join vector in vectorsInPosts
                     on post.Id equals vector.Id
                     select new BlossomPostWithVector(post, vector);

        return result.ToList();
    }

    public async Task<List<BlossomSpaceWithVector>> GetAsync(IEnumerable<BlossomSpace> spaces)
    {
        var spaceIds = spaces.Select(x => x.SpaceId).ToList();

        var vectorsInSpaces = await vectors.Query
            .Where(x => x.Type == "Space" && spaceIds.Contains(x.Id))
            .ToListAsync();

        var result = from space in spaces
                     join vector in vectorsInSpaces
                     on space.Id equals vector.Id
                     select new BlossomSpaceWithVector(space, vector);

        return result.ToList();
    }

    public async Task UpdateAsync(BlossomVector vector) => await vectors.UpdateAsync(vector);
    public async Task UpdateAsync(IEnumerable<BlossomVector> blossomVectors) => await vectors.UpdateAsync(blossomVectors);
    public async Task DeleteAsync(IEnumerable<BlossomVector> blossomVectors) => await vectors.DeleteAsync(blossomVectors);

    public async Task<List<BlossomVector>> SearchAsync(BlossomVector vector, string type, int count, bool furthestAway = false, bool includeVectors = false, double? similarityThreshold = null)
    { 
        var top = furthestAway ? 10000 : count;
        var includeVectorClause = includeVectors ? ", c.Vector" : string.Empty;

        var query = $@"
            SELECT TOP {top} c.id, c.Type, c.Text, c.CoherenceWeight, VectorDistance(c.Vector, {vector}) as SimilarityToSpace, c.TargetUrl{includeVectorClause}
            FROM c
            WHERE c.SpaceId = '{vector.SpaceId}' AND c.Type = '{type}'
            ORDER BY VectorDistance(c.Vector, {vector})";

        var cosmosVectors = vectors as CosmosDbSimpleRepository<BlossomVector>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<BlossomVector>(query, vector.SpaceId);

        if (furthestAway)
            similarVectorsInSpace = similarVectorsInSpace.TakeLast(count).ToList();

        if (similarityThreshold.HasValue)
            similarVectorsInSpace = similarVectorsInSpace
                .Where(x => x.SimilarityToSpace >= similarityThreshold.Value)
                .ToList();

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

    internal async Task<BlossomPostWithVector> VectorizeAsync(BlossomPost post, 
        List<BlossomPostWithVector> lookbackPosts, double lookbackWeight)
    {
        var translator = translators.OfType<OpenAITranslator>().First();
        var postWithVector = new BlossomPostWithVector(post, await translator.VectorizeAsync(post));

        foreach (var lookbackPost in lookbackPosts)
            postWithVector.Vector.Add(lookbackPost.Vector, lookbackWeight);

        var neighbors = await SearchAsync(postWithVector.Vector, "Post", 20, includeVectors: true);
        postWithVector.UpdateCoherence(neighbors);
        await vectors.UpdateAsync(postWithVector.Vector);
        
        return postWithVector;
    }

    internal async Task ClearAsync(string spaceId, string type)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId && x.Type == type).ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);
    }

    public async Task SummarizeAsync(BlossomVector vector)
    {
        var aiTranslator = translators.OfType<AITranslator>().First();
        if (vector.Type == "Facet")
        {
            var leftVectors = await SearchAsync(vector, "Post", 5, true);
            var leftVectorIds = leftVectors.Where(x => x.SimilarityToSpace < 0).Select(x => x.Id).ToList();

            var rightVectors = await SearchAsync(vector, "Post", 5);
            var rightVectorIds = rightVectors.Where(x => x.SimilarityToSpace > 0).Select(x => x.Id).ToList();

            var leftPosts = await posts.Query
                .Where(x => x.Domain == vector.SpaceId && leftVectorIds.Contains(x.Id))
                .ToListAsync();

            var rightPosts = await posts.Query
                .Where(x => x.Domain == vector.SpaceId && rightVectorIds.Contains(x.Id))
                .ToListAsync();

            // Use the similarity scores to weight the summary by most relevant posts
            foreach (var post in leftPosts)
                post.CoherenceWeight = Math.Abs(leftVectors.First(x => x.Id == post.Id).SimilarityToSpace);
            foreach (var post in rightPosts)
                post.CoherenceWeight = Math.Abs(rightVectors.First(x => x.Id == post.Id).SimilarityToSpace);

            var summary = await aiTranslator.SummarizeAsync(leftPosts, rightPosts);
            vector.SetSummary(summary);
        }
        else if (vector.Type == "Constellation")
        {
            var matchingPosts = await posts.Query
                .Where(x => x.Domain == vector.SpaceId && x.ConstellationId == vector.Id)
                .ToListAsync();

            var summary = await aiTranslator.SummarizeAsync(matchingPosts);
            vector.SetSummary(summary);
        }
        else
        {
            var closestVectors = await SearchAsync(vector, "Post", 10);
            var ids = closestVectors.Select(x => x.Id).ToList();

            var matchingPosts = await posts.Query
                .Where(x => x.Domain == vector.SpaceId && ids.Contains(x.Id))
                .ToListAsync();

            var summary = await aiTranslator.SummarizeAsync(matchingPosts);
            vector.SetSummary(summary);
        }

        await UpdateAsync(vector);
    }

    

    internal async Task<List<BlossomVector>> GetAllAsync(BlossomSpace space, string? type = null)
    {        
        return await vectors.Query
            .Where(x => x.SpaceId == space.Id && (type == null || x.Type == type))
            .ToListAsync();
    }
}

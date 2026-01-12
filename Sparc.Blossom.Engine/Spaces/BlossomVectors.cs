using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using System.Globalization;

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

    public async Task<List<BlossomPostWithVector>> GetAsync(IEnumerable<BlossomPost> posts)
    {
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

    public async Task<List<BlossomVector>> SearchAsync(string parentSpaceId, string id, string type, int count, bool furthestAway = false, bool includeVectors = false, double? similarityThreshold = null)
    {
        var spaceVector = await vectors.Query
            .Where(x => x.SpaceId == parentSpaceId && x.Id == id)
            .Select(x => x.Vector)
            .FirstOrDefaultAsync()
            ?? throw new Exception("Space vector not found");

        var top = furthestAway ? 10000 : count;
        var includeVectorClause = includeVectors ? ", c.Vector" : string.Empty;

        var query = $@"
            SELECT TOP {top} c.id, c.Type, c.Text, c.CoherenceWeight, VectorDistance(c.Vector, {new BlossomVector(spaceVector)}) as SimilarityToSpace, c.TargetUrl{includeVectorClause}
            FROM c
            WHERE c.SpaceId = '{parentSpaceId}' AND c.Type = '{type}'
            ORDER BY VectorDistance(c.Vector, {new BlossomVector(spaceVector)})";

        var cosmosVectors = vectors as CosmosDbSimpleRepository<BlossomVector>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<BlossomVector>(query, parentSpaceId);

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

    internal async Task<BlossomVector> AddAsync(BlossomPost post, BlossomSpace userSpace)
    {
        var translator = translators.OfType<OpenAITranslator>().First();
        var postWithVector = new BlossomPostWithVector(post, await translator.VectorizeAsync(post));
        var spaceVector = await GetOrCreateSpace(postWithVector);

        var neighbors = await SearchAsync(post.SpaceId, post.SpaceId, "Post", 20, includeVectors: true);
        postWithVector.Vector.CalculateCoherenceWeight(neighbors);
        post.CoherenceWeight = postWithVector.Vector.CoherenceWeight;
        //post.UserMovementWeight = post.CoherenceWeight * Math.Max(0, spaceVector.SimilarityTo(postVector) ?? 0);
        await vectors.AddAsync(postWithVector.Vector);

        await UpdateUserSpace(postWithVector, userSpace, spaceVector);

        // Update the space vector
        spaceVector.Update(postWithVector.Vector, 0.1);
        await UpdateAsync(spaceVector);
        return spaceVector;
    }

    private async Task UpdateUserSpace(BlossomPostWithVector post, BlossomSpace userSpace, BlossomVector spaceVector)
    {
        var userVector = await FindAsync(post.Post.SpaceId, userSpace.SpaceId);
        if (userVector == null)
            userVector = new BlossomVector(post.Post.SpaceId, "User", userSpace.SpaceId, post.Vector.Vector)
            {
                CoherenceWeight = post.Post.CoherenceWeight
            };
        else
        {
            var oldCoherenceWeight = userVector.CoherenceWeight;

            // compute movement strength: product of coherence and alignment with space axis
            var alignmentWithSpace = Math.Max(0, spaceVector.SimilarityTo(post.Vector) ?? 0);
            var movementStrength = post.Post.CoherenceWeight * alignmentWithSpace;

            // alpha controls how far the user headspace moves toward this post (tunable)
            var alpha = Math.Clamp(0.1 * movementStrength, 0.0, 1.0);

            // interpolate then normalize
            userVector = userVector.InterpolateTowards(post.Vector, alpha);

            // update coherence weight (simple accumulation; you can change to a decay/avg if desired)
            userVector.CoherenceWeight = oldCoherenceWeight + post.Post.CoherenceWeight;

        }
        await UpdateAsync(userVector);
    }

    private async Task<BlossomVector> GetOrCreateSpace(BlossomPostWithVector post)
    {
        var spaceId = post.Post.SpaceId;
        var spaceVector = await FindAsync(spaceId, spaceId);
        if (spaceVector == null)
        {
            spaceVector = new BlossomVector(spaceId, "Space", spaceId, post.Vector.Vector);
            await vectors.AddAsync(spaceVector);
        }

        return spaceVector;
    }

    internal async Task ClearAsync(string spaceId, string type)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId && x.Type == type).ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);
    }
}

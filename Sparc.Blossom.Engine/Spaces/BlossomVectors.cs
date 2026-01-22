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

    public async Task<List<BlossomVector>> SearchAsync(BlossomSpace space, string type, int count, bool furthestAway = false, bool includeVectors = false, double? similarityThreshold = null)
    {
        var spaceVector = await FindAsync(space)
            ?? throw new Exception("Space vector not found");

        return await SearchAsync(spaceVector, type, count, furthestAway, includeVectors, similarityThreshold);
    }

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

    internal async Task<BlossomPostWithVector> VectorizeAsync(BlossomPost post, List<BlossomPost> lookbackPosts)
    {
        var translator = translators.OfType<OpenAITranslator>().First();
        var postWithVector = new BlossomPostWithVector(post, await translator.VectorizeAsync(post, lookbackPosts));
        var neighbors = await SearchAsync(postWithVector.Vector, "Post", 20, includeVectors: true);
        postWithVector.UpdateCoherence(neighbors);
        await vectors.UpdateAsync(postWithVector.Vector);
        return postWithVector;
    }

    internal async Task<BlossomSpaceWithVector> UpdateSpaceHeadspace(BlossomSpace space, BlossomPostWithVector post)
    {
        var spaceVector = await FindAsync(space);
        if (spaceVector == null)
        {
            spaceVector = new BlossomVector(space, post.Vector.Vector);
            await vectors.AddAsync(spaceVector);
        }

        spaceVector.Update(post.Vector, space.Settings.SpaceGravity);
        await UpdateAsync(spaceVector);

        var vectorizedSpace = new BlossomSpaceWithVector(space, spaceVector);
        return vectorizedSpace;
    }

    internal async Task<BlossomVector> UpdateUserHeadspace(BlossomSpace space, BlossomPostWithVector post)
    {
        var userVector = await FindAsync(post.Post.SpaceId, post.Post.User!.Id);
        if (userVector == null)
        {
            userVector = new BlossomVector(post.Post.SpaceId, "User", post.Post.User.Id, post.Vector.Vector);
            await vectors.AddAsync(userVector);
        }
        else
        {

            userVector.Update(post.Vector, space.Settings.HeadspaceVelocity);
            await UpdateAsync(userVector);
        }

        return userVector;
    }

    internal async Task ClearAsync(string spaceId, string type)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId && x.Type == type).ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);
    }

    internal async Task<List<BlossomVector>> GetAllAsync(BlossomSpace space, string? type = null)
    {
        var spaceIds = space.LinkedSpaces
            .Where(x => type == null || x.Type == type)
            .Select(x => x.SpaceId)
            .ToList();
        
        spaceIds.Add(space.Id);

        return await vectors.Query
            .Where(x => x.SpaceId == space.Id && spaceIds.Contains(x.Id))
            .ToListAsync();
    }
}

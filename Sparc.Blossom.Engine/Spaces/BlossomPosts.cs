using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomPosts(IRepository<Post> posts,       
    VoyageTranslator translator)
{
    internal async Task<Post> VectorizeAsync(Post post, BlossomSpace space)
    {
        await translator.VectorizeAsync(post);

        var lookbackPosts = await GetAllAsync(space, space.Settings.MessageLookback);
        if (lookbackPosts != null)
            foreach (var lookbackPost in lookbackPosts)
                post.Vector.Update(lookbackPost.Vector, space.Settings.MessageLookbackWeight);

        var neighbors = await posts.SearchAsync(post.SpaceId, post.Vector, 20);
        post.Vector.CalculateLocalCoherence(neighbors.Select(x => x.Item.Vector).ToList());
        await posts.UpdateAsync(post);

        return post;
    }

    internal async Task<Post> AddAsync(Post post, BlossomSpace space)
    {
        post.SpaceId = space.Id;

        await VectorizeAsync(post, space);
        await posts.AddAsync(post);

        return post;
    }

    internal async Task<List<Post>> GetAllAsync(BlossomSpace space, int take = 50) => await GetAllAsync(space.Id, take);

    internal async Task<List<Post>> GetAllAsync(string spaceId, int take = 50)
    {
        if (take == 0)
            return [];

        return await posts.Query.Where(x => x.SpaceId == spaceId)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync();
    }

    internal async Task<List<VectorSearchResult<Post>>> SearchAsync(string spaceId, BlossomVector vector, int count, double? similarityThreshold = null)
    {
        var allPosts = await GetAllAsync(spaceId, 10000);
        var result = allPosts.Select(p => new VectorSearchResult<Post>(p, p.Vector.SimilarityTo(vector)));

        if (similarityThreshold == null)
            return result.OrderByDescending(x => x.Score).Take(count).ToList();

        if (similarityThreshold < 0)
            return result.Where(x => x.Score < 0).OrderBy(x => x.Score).Take(count).ToList();
        else
            return result.Where(x => x.Score > 0).OrderBy(x => x.Score).Take(count).ToList();
    }

    internal async Task UpdateAsync(IEnumerable<Post> postsToUpdate) => await posts.UpdateAsync(postsToUpdate);
}

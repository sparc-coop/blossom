using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomPosts(IRepository<Post> posts,
        IRepository<Headspace> headspaces,
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
        post.Vector.CalculateCoherenceWeight(neighbors.Select(x => x.Item.Vector).ToList());
        await posts.UpdateAsync(post);

        return post;
    }

    internal async Task<Post> AddAsync(Post post, BlossomSpace space)
    {
        var headspace = await GetCurrentHeadspace(space, post.User);

        await VectorizeAsync(post, space);
        headspace = new(space, post, headspace);
        await headspaces.AddAsync(headspace);

        await posts.AddAsync(post);

        return post;
    }

    internal async Task<List<Post>> GetAllAsync(BlossomSpace space, int take = 50)
    {
        if (take == 0)
            return [];
        
        return await posts.Query.Where(x => x.SpaceId == space.Id)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync();
    }

    private async Task<Headspace?> GetCurrentHeadspace(BlossomSpace space, BlossomAvatar user)
    {
        var headspace = await headspaces.Query
            .Where(x => x.SpaceId == space.Id && x.User.Id == user.Id)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        return headspace;
    }

    internal async Task<List<VectorSearchResult<Post>>> SearchAsync(string spaceId, BlossomVector vector, int count, double? similarityThreshold = null)
    {
        return await posts.SearchAsync(spaceId, vector, count, similarityThreshold);
    }

    internal async Task UpdateAsync(IEnumerable<Post> postsToUpdate) => await posts.UpdateAsync(postsToUpdate);
}

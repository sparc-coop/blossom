using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomPosts(IRepository<Post> posts,
    IRepository<Fact> guides,
    IRepository<Media> medias,
    VoyageTranslator translator,
    AzureBlobRepository blobs)
{
    internal async Task VectorizeAsync(BlossomSpark spark, BlossomSpace space)
    {
        await translator.VectorizeAsync(spark);

        var lookbackPosts = await GetAllAsync(space, space.Settings.MessageLookback);
        if (lookbackPosts != null)
            foreach (var lookbackPost in lookbackPosts)
                spark.Vector.Update(lookbackPost.Vector, space.Settings.MessageLookbackWeight);

        var neighbors = await posts.SearchAsync(spark.RealmId, spark.Vector, 20);
        spark.Vector.CalculateLocalCoherence(neighbors.Select(x => x.Item.Vector).ToList());

        if (spark is Post post)
            await posts.UpdateAsync(post);
        else if (spark is Media media)
            await medias.UpdateAsync(media);
    }

    internal async Task<Post> AddAsync(Post post, BlossomSpace space, BlossomSpace userSpace)
    {
        post.RealmId = space.Id;
        post.User = userSpace.User;

        await VectorizeAsync(post, space);
        return post;
    }

    internal async Task<Media> AddAsync(Media media, BlossomSpace space, BlossomSpace userSpace)
    {
        await medias.UpdateAsync(media);
        //await VectorizeAsync(media, space);
        return media;
    }

    internal async Task<List<Post>> GetAllAsync(BlossomSpace space, int take = 50) => await GetAllAsync(space.Id, take);

    internal async Task<List<Post>> GetAllAsync(string spaceId, int take = 50)
    {
        if (take == 0)
            return [];

        var result = await posts.Query.Where(x => x.RealmId == spaceId)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync();
        
        return result;
    }

    internal async Task<List<BlossomScoredVector<Fact>>> SearchAsync(BlossomSpace space, BlossomVector vector, int count)
    {
        var result = await guides.SearchAsync(space.Id, vector, count);
        return result;
    }

    internal async Task UpdateAsync(IEnumerable<Post> postsToUpdate) => await posts.UpdateAsync(postsToUpdate);

    internal async Task<List<Post>> GetAllAsync(BlossomSpace space, BlossomAvatar user, int take)
    {
        var result = await posts.Query
            .Where(x => x.RealmId == space.Id && x.User.Id == user.Id)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync();

        return result;
    }
}

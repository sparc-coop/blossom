using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaces(
    BlossomAggregateOptions<BlossomSpace> options,
    BlossomPosts posts,
    BlossomSpaceFaceter faceter,
    BlossomSpaceConstellator constellator,
    BlossomSpaceTranslator translator,
    IRepository<BlossomUserTrail> headspaces,
    BlossomGameStates gameStates)
    : BlossomAggregate<BlossomSpace>(options), IBlossomEndpoints
{
    public const string Domain = "sparc.coop";

    private async Task<List<BlossomSpace>> GetSpacesAsync(string? parentSpaceId = null, int? limit = null, string? type = null)
    {
        parentSpaceId ??= Domain;

        var spaces = Repository.Query
            .Where(x => x.SpaceId == parentSpaceId);

        if (type != null)
            spaces = spaces.Where(x => x.RoomType == type);

        return await spaces.OrderByDescending(x => x.Timestamp).ToListAsync();
    }

    internal async Task<BlossomSpace?> GetSpaceAsync(ClaimsPrincipal principal, string spaceId, string? parentSpaceId = null)
    {
        parentSpaceId ??= Domain;
        if (spaceId == "User")
            spaceId = principal.Id();

        return await Repository.FindAsync(parentSpaceId, spaceId);
    }

    private async Task<BlossomSpace> GetOrCreate(string? spaceId, string roomType = "Space", string? parentSpaceId = null, BlossomAvatar? user = null)
    {
        parentSpaceId ??= Domain;

        if (string.IsNullOrWhiteSpace(spaceId))
            spaceId = Guid.NewGuid().ToString();

        var existing = await Repository.FindAsync(parentSpaceId, spaceId);
        if (existing == null)
        {
            existing = new BlossomSpace(spaceId, roomType) { SpaceId = parentSpaceId, User = user ?? BlossomUser.System.Avatar };
            await Repository.AddAsync(existing);
        }

        return existing;
    }

    private async Task<BlossomSpace> GetOrCreateUserSpace(BlossomSpace space, BlossomAvatar user)
    {
        return await GetOrCreate(user.Id, "User", space.Id, user);
    }


    private async Task<Post> PostAsync(string spaceId, Post post)
    {
        var space = await GetOrCreate(post.SpaceId, user: post.User);
        var userSpace = await GetOrCreateUserSpace(space, post.User);
        var isFirstPost = space.Vector.IsEmpty;

        var previousPosts = await posts.GetAllAsync(space, 1000);

        post = await posts.AddAsync(post, space);

        await Repository.ExecuteAsync(space, x => x.Add(post, previousPosts));

        await Repository.ExecuteAsync(userSpace, x => x.Add(post, previousPosts));

        var headspace = new BlossomUserTrail(space, userSpace);
        await headspaces.AddAsync(headspace);

        if (isFirstPost)
        {
            var guides = await translator.SeedAsync(space, post);
            await faceter.SeedAsync(space, guides);
            await Repository.UpdateAsync(space);
        }
        else
        {
            await SaveAsync(spaceId, space);
        }

        //var hintVectors = await vectors.GetAllAsync(space.Space.Id, "Hint");
        //var hint = await vectors.CalculateHintAsync(userSpace, post, space);
        //await posts.AddAsync(hint);

        return post;
    }

    private async Task SaveAsync(string spaceId, BlossomSpace space)
    {
        var existing = await GetOrCreate(spaceId);
        existing.Settings = space.Settings;
        await Repository.UpdateAsync(existing);

        await faceter.FacetAsync(existing);
        await Repository.UpdateAsync(existing);

        await constellator.ConstellateAsync(existing);
        //await vectors.SummarizeAsync(existing.Vector);
        //existing.Space.SetSummary(existing.Vector.Summary);
        //await Repository.UpdateAsync(existing.Space);
    }

    private async Task<List<Post>> GetPostsAsync(string spaceId, string type = "Post", int take = 50)
    {
        var space = await GetOrCreate(Domain, spaceId);
        return await posts.GetAllAsync(space, take);
    }

    private async Task<GameState> GetCoordinatesAsync(ClaimsPrincipal principal, string spaceId)
    {
        try
        {
            var space = await GetOrCreate(spaceId);
            var userSpace = await GetOrCreate(principal.Id(), "User", spaceId);

            var state = await gameStates.GetCoordinatesAsync(space, userSpace);
            return state;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message + e.InnerException?.Message);
            Console.WriteLine(e.Message + e.InnerException?.Message);
            return new(new BlossomSpace(e.Message + e.InnerException?.Message + e.StackTrace), null, null, null, null, null, null, 0);
        }
    }

    private async Task ActivateQuestAsync(ClaimsPrincipal principal, string spaceId, string facetId)
    {
        var space = await GetOrCreate(spaceId);
        var userSpace = await GetOrCreate(principal.Id(), "User", spaceId);

        if (userSpace.ActiveQuestId != null)
        {
            userSpace.DeactivateQuest();
        }
        else
        {
            var quest = await faceter.ActivateQuestAsync(space, userSpace, facetId);
        }

        await Repository.UpdateAsync(userSpace);
    }

    private async Task<string> GetSimplePostsAsync(string spaceId)
    {
        var posts = await GetPostsAsync(spaceId);
        return string.Join("\r\n\r\n----------------------------------------------------------------\r\n\r\n", posts.Select(x => $"({x.Timestamp}) {x.User?.Username}: {x.Text}"));
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var spaces = endpoints.MapGroup("/spaces");

        spaces.MapGet("", GetSpacesAsync);
        spaces.MapGet("{spaceId}", GetSpaceAsync);
        spaces.MapGet("{parentSpaceId}/subspaces/{spaceId}", GetSpaceAsync);
        spaces.MapGet("{spaceId}/posts", GetPostsAsync);
        spaces.MapGet("{spaceId}/simpleposts", GetSimplePostsAsync);
        spaces.MapGet("{spaceId}/coordinates", async (ClaimsPrincipal principal, string spaceId)
            => await GetCoordinatesAsync(principal, spaceId));
        spaces.MapPost("{spaceId}", PostAsync);
        spaces.MapPost("{spaceId}/quests/{facetId}", async (ClaimsPrincipal principal, string spaceId, string facetId)
            => await ActivateQuestAsync(principal, spaceId, facetId));
        spaces.MapPut("{spaceId}", SaveAsync);
    }
}
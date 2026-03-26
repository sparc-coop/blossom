using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaces(
    BlossomAggregateOptions<BlossomSpace> options,
    BlossomPosts posts,
    BlossomSpaceQuests quests,
    BlossomSpaceTranslator translator,
    BlossomSpaceObjects objects,
    IBlossomAuthenticator auth)
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

    private async Task<BlossomSpace> CreateAsync(Post post)
    {
        var (space, userSpace) = await GetCurrentSpaces(post.SpaceId);

        post.Vector.Text = post.Text;
        userSpace.Vector = await translator.VectorizeAsync(post) ?? new();
        space.Vector = userSpace.Vector;
        
        await Repository.UpdateAsync(space);
        await Repository.UpdateAsync(userSpace);

        var seeds = await translator.SeedAsync(space, post, 20);
        await quests.FacetAsync(space, seeds);
        await quests.FindQuestsAsync(space, userSpace);
        await objects.RecalculateAsync(space);

        return space;
    }

    private async Task<BlossomSpace> GetOrCreate(string? spaceId, string roomType = "Space", string? parentSpaceId = null, BlossomAvatar? user = null)
    {
        parentSpaceId ??= Domain;

        if (string.IsNullOrWhiteSpace(spaceId))
            spaceId = Guid.NewGuid().ToString();

        var existing = await Repository.FindAsync(parentSpaceId, spaceId);
        if (existing == null)
        {
            user ??= (await auth.GetAsync(User))?.Avatar;
            existing = new BlossomSpace(spaceId, roomType) { SpaceId = parentSpaceId, User = user ?? BlossomUser.System.Avatar };
            await Repository.AddAsync(existing);
        }

        return existing;
    }

    private async Task<Post> PostAsync(string spaceId, Post post)
    {
        var (space, userSpace) = await GetCurrentSpaces(spaceId);
        post = await posts.AddAsync(post, space, userSpace);
        await objects.RecalculateAsync(space);

        return post;
    }

    private async Task SaveAsync(string spaceId, BlossomSpace space)
    {
        var (existing, userSpace) = await GetCurrentSpaces(spaceId);
        existing.Settings = space.Settings;
        await Repository.UpdateAsync(existing);

        await objects.RecalculateAsync(existing);
    }

    private async Task<List<Post>> GetPostsAsync(string spaceId, string type = "Post", int take = 50)
    {
        var space = await GetOrCreate(Domain, spaceId);
        return await posts.GetAllAsync(space, take);
    }

    private async Task<GameState> GetCoordinatesAsync(string spaceId)
    {
        var (space, userSpace) = await GetCurrentSpaces(spaceId);
        return await translator.GetCoordinatesAsync(space, userSpace);
    }

    private async Task<List<QuestPath>> TravelAsync(string spaceId, string originId)
    {
        var (space, userSpace) = await GetCurrentSpaces(spaceId);
        var quest = new Quest(space, userSpace);
        var spaceObjects = await objects.GetAllAsync(space);
        if (originId == "self") originId = userSpace.Id;
        var origin = spaceObjects.FirstOrDefault(o => o.Id == originId);
        if (origin == null)
            return [];

        var path = quest.Travel(origin, spaceObjects, 100, 0.5f, 2f);
        path.ForEach(x => x.MaterializeCoordinates(space.Axes));
        return path;
    }

    private async Task ActivateQuestAsync(string spaceId, string facetId)
    {
        var (space, userSpace) = await GetCurrentSpaces(spaceId);
        await quests.ActivateQuestAsync(space, userSpace);
    }

    private async Task<(BlossomSpace space, BlossomSpace userSpace)> GetCurrentSpaces(string spaceId)
    {
        var space = await GetOrCreate(spaceId);
        var userSpace = await GetOrCreate(User.Id(), "User", spaceId);
        return (space, userSpace);
    }

    private async Task DeleteSpaceAsync(string spaceId)
    {
        await objects.DeleteAsync(spaceId);

        var space = await Repository.FindAsync(Domain, spaceId);
        if (space != null)
            await Repository.DeleteAsync(space);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var spaces = endpoints.MapGroup("/spaces");

        spaces.MapGet("", GetSpacesAsync);
        spaces.MapPost("", async (Post post) => await CreateAsync(post));
        spaces.MapGet("{spaceId}", GetSpaceAsync);
        spaces.MapGet("{parentSpaceId}/subspaces/{spaceId}", GetSpaceAsync);
        spaces.MapGet("{spaceId}/posts", GetPostsAsync);
        spaces.MapGet("{spaceId}/coordinates", async (string spaceId) => await GetCoordinatesAsync(spaceId));
        spaces.MapGet("{spaceId}/travel/{originId}", async (string spaceId, string originId) => await TravelAsync(spaceId, originId));
        spaces.MapPost("{spaceId}", async (string spaceId, Post post) => await PostAsync(spaceId, post));
        spaces.MapPost("{spaceId}/quests/{facetId}", async (string spaceId, string facetId) => await ActivateQuestAsync(spaceId, facetId));
        spaces.MapDelete("{spaceId}", async (string spaceId) => await DeleteSpaceAsync(spaceId));
        spaces.MapPut("{spaceId}", SaveAsync);
    }
}
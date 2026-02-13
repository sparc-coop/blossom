using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaces(
    BlossomAggregateOptions<BlossomSpace> options,
    BlossomPosts posts,
    IRepository<BlossomSpaceObject> allObjects,
    IRepository<Quest> quests,
    BlossomSpaceFaceter faceter,
    BlossomSpaceConstellator constellator,
    BlossomSpaceTranslator translator)
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

        return await spaces.ToListAsync();
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

    public record GraphExtractionResult(List<SparcEntityBase> Entities, List<SparcRelationship> Relationships);
    public async Task<List<SparcEntity>> ExtractGraph(ExtractGraphRequest request)
    {
        var options = new TranslationOptions
        {
            Instructions = SparcPrompts.GraphExtraction(request.EntityTypes),
            Schema = new BlossomSchema(typeof(GraphExtractionResult))
        };

        //var graph = await contents.TranslateAsync<GraphExtractionResult>(request.Content, options);
        //if (graph?.Entities == null)
        return [];

        //var entities = graph.Entities.Select(x => new SparcEntity(x, graph.Relationships));
        //return entities.ToList();
    }

    private async Task<Post> PostAsync(string spaceId, Post post)
    {
        var space = await GetOrCreate(post.SpaceId, user: post.User);
        var isFirstPost = space.Vector.IsEmpty;

        post = await posts.AddAsync(post, space);

        await Repository.ExecuteAsync(space, x => x.Add(post));

        if (isFirstPost)
            await translator.SeedAsync(space, post);

        await SaveAsync(spaceId, space);

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

        var facets = await faceter.FacetAsync(space);
        var axes = space.MaterializeAxes(facets);
        await constellator.ConstellateAsync(existing, axes);
        //await vectors.SummarizeAsync(existing.Vector);
        //existing.Space.SetSummary(existing.Vector.Summary);
        //await Repository.UpdateAsync(existing.Space);
    }

    private async Task<List<Post>> GetPostsAsync(string spaceId, string type = "Post", int take = 50)
    {
        var space = await GetOrCreate(Domain, spaceId);
        return await posts.GetAllAsync(space, take);
    }

    private async Task<GameState> GetCoordinatesAsync(ClaimsPrincipal principal, string spaceId, string? questId = null)
    {
        var space = await GetOrCreate(spaceId);
        var userId = principal.Id();
        var gameObjects = await allObjects.Query.Where(x => x.SpaceId == spaceId).ToListAsync();

        var headspace = gameObjects
            .OfType<Headspace>()
            .Where(x => x.User.Id == userId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

        var distanceToAnswer = headspace?.Vector.DistanceTo(space.Vector) ?? 0;

        var facets = gameObjects.OfType<Facet>();
        if (headspace != null)
        {
            foreach (var availableQuest in gameObjects.OfType<Facet>())
                availableQuest.CheckForQuest(space, headspace, distanceToAnswer);
        }

        var axes = space.MaterializeAxes(facets);

        if (questId != null)
        {
            var selectedQuest = gameObjects.OfType<Quest>().FirstOrDefault(x => x.Id == questId);
            if (selectedQuest == null)
            {
                var selectedFacet = gameObjects.OfType<Facet>().First(x => x.Id == questId);
                selectedQuest = new Quest(space, selectedFacet, headspace!.User);
                await quests.AddAsync(selectedQuest);
            }

            axes = selectedQuest.MaterializeAxes(space, gameObjects, axes);
        }

        gameObjects.ForEach(x => x.MaterializeCoordinates(axes));
        return new(gameObjects, distanceToAnswer);
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
        spaces.MapGet("{spaceId}/coordinates", async (ClaimsPrincipal principal, string spaceId, string? questId = null)
            => await GetCoordinatesAsync(principal, spaceId, questId));
        spaces.MapPost("{spaceId}", PostAsync);
        spaces.MapPut("{spaceId}", SaveAsync);
        spaces.MapPost("graph", async (BlossomSpaces spaces, ExtractGraphRequest request) =>
        {
            var result = await spaces.ExtractGraph(request);
            return Results.Ok(result);
        });
    }
}
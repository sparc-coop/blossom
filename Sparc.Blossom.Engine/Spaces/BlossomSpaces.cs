using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaces(
    BlossomAggregateOptions<BlossomSpace> options,
    IRepository<BlossomPost> posts,
    IRepository<BlossomEvent> events,
    IHttpContextAccessor http,
    BlossomVectors vectors,
    Contents contents,
    BlossomSpaceFaceter faceter,
    BlossomSpaceConstellator constellator,
    SparcAuthenticator<BlossomUser> auth)
    : BlossomAggregate<BlossomSpace>(options), IBlossomEndpoints
{
    public const string Domain = "sparc.coop";
    public string? MatrixSenderId;

    private async Task<BlossomPresence> GetPresenceAsync(ClaimsPrincipal principal, string userId)
    {
        var user = await auth.GetAsync(principal);
        return new BlossomPresence(user.Avatar);
    }

    private async Task SetPresenceAsync(ClaimsPrincipal principal, string userId, BlossomPresence presence)
    {
        var user = await auth.GetAsync(principal);
        presence.ApplyToAvatar(user.Avatar);
        user.UpdateAvatar(user.Avatar);
        await auth.UpdateAsync(user);
        await PublishAsync(userId, presence);
    }

    private async Task<List<BlossomSpace>> GetSpacesAsync(string? parentSpaceId = null, int? limit = null, string? type = null)
    {
        var spaces = parentSpaceId == null
            ? Repository.Query
            .Where(x => x.Domain == Domain && x.RoomType == "Root")
            : Repository.Query
            .Where(x => x.Domain == parentSpaceId);

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

    private async Task<BlossomSpaceWithVector> GetOrCreate(string? spaceId, string roomType = "Space", string? parentSpaceId = null)
    {
        parentSpaceId ??= Domain;

        if (string.IsNullOrWhiteSpace(spaceId))
            spaceId = Guid.NewGuid().ToString();

        var existing = await Repository.FindAsync(parentSpaceId, spaceId);
        if (existing == null)
        {
            existing = new BlossomSpace(parentSpaceId, spaceId, roomType);
            await Repository.AddAsync(existing);
        }

        var vector = await vectors.FindAsync(existing) ?? new(existing, []);

        return new(existing, vector);
    }

    private async Task<CreateSpaceResponse> CreateSpaceAsync(CreateSpaceRequest request)
    {
        var spaceId = "!" + BlossomEvent.OpaqueId() + ":" + Domain;
        await PublishAsync(spaceId, new CreateSpace());
        await PublishAsync(spaceId, new ChangeMembershipState("join", MatrixSenderId!));
        await PublishAsync(spaceId, new AdjustPowerLevels());

        if (!string.IsNullOrWhiteSpace(request.RoomAliasName))
            await PublishAsync(spaceId, new CanonicalAlias(request.RoomAliasName));

        if (!string.IsNullOrWhiteSpace(request.Preset))
        {
            switch (request.Preset)
            {
                case "public_chat":
                    await PublishAsync(spaceId, new JoinRules("public"));
                    await PublishAsync(spaceId, HistoryVisibility.Shared);
                    await PublishAsync(spaceId, GuestAccess.Forbidden);
                    break;
                case "private_chat":
                case "trusted_private_chat":
                    await PublishAsync(spaceId, new JoinRules("invite"));
                    await PublishAsync(spaceId, HistoryVisibility.Shared);
                    await PublishAsync(spaceId, GuestAccess.CanJoin);
                    break;
                default:
                    throw new NotSupportedException($"Preset '{request.Preset}' is not supported.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            await PublishAsync(spaceId, new RoomName(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Topic))
            await PublishAsync(spaceId, new RoomTopic(request.Topic));

        if (request.Invite?.Count > 0)
        {
            foreach (var userId in request.Invite)
                await PublishAsync(spaceId, new ChangeMembershipState("invite", userId));
        }

        return new(spaceId);
    }

    private async Task JoinSpaceAsync(string spaceId)
    {
        await PublishAsync(spaceId, new ChangeMembershipState("join", MatrixSenderId!));
    }

    private async Task LeaveSpaceAsync(string spaceId)
    {
        await PublishAsync(spaceId, new ChangeMembershipState("leave", MatrixSenderId!));
    }

    private async Task InviteToSpaceAsync(string spaceId, InviteToSpaceRequest request)
    {
        await PublishAsync(spaceId, new ChangeMembershipState("invite", request.UserId));
    }

    public record GraphExtractionResult(List<SparcEntityBase> Entities, List<SparcRelationship> Relationships);
    public async Task<List<SparcEntity>> ExtractGraph(ExtractGraphRequest request)
    {
        var options = new TranslationOptions
        {
            Instructions = SparcPrompts.GraphExtraction(request.EntityTypes),
            Schema = new BlossomSchema(typeof(GraphExtractionResult))
        };

        var graph = await contents.TranslateAsync<GraphExtractionResult>(request.Content, options);
        if (graph?.Entities == null)
            return [];

        var entities = graph.Entities.Select(x => new SparcEntity(x, graph.Relationships));
        return entities.ToList();
    }

    public async Task<List<BlossomSpace>> Discover(string spaceId)
    {
        var existing = await Repository.Query.Where(x => x.Domain == spaceId).ToListAsync();
        await Repository.DeleteAsync(existing);

        var space = await GetOrCreate(spaceId);
        var spacePosts = await GetPostsAsync(spaceId, take: 10000);

        await posts.UpdateAsync(spacePosts);
        await Repository.UpdateAsync(space.Space);
        return [];
    }

    private async Task<BlossomPost> PostAsync(string spaceId, BlossomPost post)
    {
        var space = await GetOrCreate(post.SpaceId);
        var userSpace = await GetOrCreate(post.User!.Id, "User", space.Space.Id);
        var isFirstPost = space.Vector.IsEmpty;

        var allPosts = await GetPostsWithVectorsAsync(spaceId, 10000);
        var lookbackPosts = allPosts.OrderByDescending(x => x.Post.Timestamp)
            .Take(space.Space.Settings.MessageLookback)
            .ToList();

        var postWithVector = await vectors.VectorizeAsync(post, lookbackPosts, space.Space.Settings.MessageLookbackWeight);

        userSpace.Add(postWithVector);
        await vectors.UpdateAsync(userSpace.Vector);

        var userTrail = new BlossomVector(space.Space.Id, "UserTrail", Guid.NewGuid().ToString(), userSpace.Vector.Vector);
        await vectors.UpdateAsync(userTrail);

        await posts.AddAsync(post);

        if (isFirstPost)
        {
            // Generate exploratory axes based on the initial question
            await vectors.InitializeSpaceAsync(space, postWithVector);
            await Repository.UpdateAsync(space.Space);
        }
        else
        {
            space.Add(postWithVector);
            await vectors.UpdateAsync(space.Vector);

            await SaveSpaceAsync(spaceId, space.Space);
        }

        //var hintVectors = await vectors.GetAllAsync(space.Space.Id, "Hint");
        //var hint = await vectors.CalculateHintAsync(userSpace, post, space);
        //await posts.AddAsync(hint);

        return post;
    }

    private async Task SaveSpaceAsync(string spaceId, BlossomSpace space)
    {
        var existing = await GetOrCreate(spaceId);
        existing.Space.Settings = space.Settings;
        await Repository.UpdateAsync(existing.Space);

        var allPosts = await GetPostsWithVectorsAsync(spaceId, 10000);
        var facets = await faceter.FacetAsync(allPosts.Select(x => x.Vector));
        
        var axes = await vectors.GetAxesAsync(existing);
        await constellator.ConstellateAsync(existing.Space, allPosts, axes);
        //await vectors.SummarizeAsync(existing.Vector);
        //existing.Space.SetSummary(existing.Vector.Summary);
        //await Repository.UpdateAsync(existing.Space);
    }

    private async Task ActivateQuest()
    {
        //if (!userSpace.Space.LinkedSpaces.Any(x => x.Type == "Quest"))
        //{
        //    var quest = facets
        //        .Where(x => userSpace.Space.PositionOnAxis(space.Space) >= x.Space.PositionOnAxis(space.Space))
        //        .OrderBy(x => x.Space.PositionOnAxis(space.Space))
        //        .FirstOrDefault();

        //    if (quest != null)
        //    {
        //        quest.Space.RoomType = "Quest";
        //        await SummarizeAsync(quest);
        //        quest.Space.Domain = userSpace.Space.Id;
        //        quest.LinkToSpace(userSpace, facets);
        //        await Repository.UpdateAsync(quest.Space);
        //    }
        //}
    }

    private async Task<List<BlossomPost>> GetPostsAsync(string spaceId, string type = "Post", int take = 50)
    {
        var space = await GetOrCreate(Domain, spaceId);
        var exactPosts = await posts.Query
                .Where(x => x.Domain == spaceId && x.ContentType == type)
                .OrderByDescending(x => x.Timestamp)
                .Take(take)
                .ToListAsync();

        return exactPosts;
    }

    private async Task<List<BlossomCoordinate>> GetCoordinatesAsync(string spaceId, string? questId = null)
    {
        var space = await GetOrCreate(spaceId);
        var allVectors = await vectors.GetAllAsync(spaceId);
        var user = allVectors.First(x => x.Type == "User");

        allVectors.Add(space.Vector);
        var answer = space.Vector.ThisWith(space.Vector.Vector, "Answer");
        answer.Id = Guid.NewGuid().ToString();
        allVectors.Add(answer);

        foreach (var availableQuest in allVectors.Where(x => x.Type == "Facet"))
            availableQuest.ConvertToQuest(space.Vector, user);

        if (questId != null)
        {
            var selectedQuest = allVectors.First(x => x.Id == questId);
            return allVectors.Select(x => x.ToCoordinate([selectedQuest])).ToList();
        }

        var axes = await vectors.GetAxesAsync(space, allVectors);
        return allVectors.Select(x => x.ToCoordinate(axes)).ToList();
    }

    private async Task<List<BlossomPostWithVector>> GetPostsWithVectorsAsync(string spaceId, int take = 50)
    {
        var posts = await GetPostsAsync(spaceId, take: take);
        return await vectors.GetAsync(posts);
    }

    private async Task<string> GetSimplePostsAsync(string spaceId)
    {
        var posts = await GetPostsAsync(spaceId);
        return string.Join("\r\n\r\n----------------------------------------------------------------\r\n\r\n", posts.Select(x => $"({x.Timestamp}) {x.User?.Username}: {x.Text}"));
    }

    private async Task<BlossomEvent> PublishAsync<T>(string spaceId, T content)
    {
        var sender = await GetMatrixSenderIdAsync();

        var ev = BlossomEvent.Create(spaceId, sender, content);
        await events.AddAsync(ev);
        return ev;
    }

    private async Task<string> GetMatrixSenderIdAsync()
    {
        if (MatrixSenderId != null)
            return MatrixSenderId;

        var principal = http.HttpContext?.User
            ?? throw new InvalidOperationException("User not authenticated");

        var user = await auth.GetAsync(principal);

        // Ensure the user has a Matrix identity
        var username = user.Avatar.Username.ToLowerInvariant();
        var matrixId = $"@{username}:{Domain}";

        if (!user.HasIdentity("Matrix"))
        {
            user.AddIdentity("Matrix", matrixId);
            await auth.UpdateAsync(user);
        }

        return user.Identity("Matrix")!;
    }

    private async Task IndexAsync(string spaceId)
    {
        await vectors.IndexAsync(spaceId, 10000, 5);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var spaces = endpoints.MapGroup("/spaces");

        spaces.MapGet("", GetSpacesAsync);
        spaces.MapPost("", CreateSpaceAsync);
        spaces.MapGet("{spaceId}", GetSpaceAsync);
        spaces.MapGet("{parentSpaceId}/subspaces/{spaceId}", GetSpaceAsync);
        spaces.MapPost("{spaceId}/join", JoinSpaceAsync);
        spaces.MapPost("{spaceId}/leave", LeaveSpaceAsync);
        spaces.MapPost("{spaceId}/invite", InviteToSpaceAsync);
        spaces.MapGet("{spaceId}/posts", GetPostsAsync);
        spaces.MapGet("{spaceId}/simpleposts", GetSimplePostsAsync);
        spaces.MapGet("{spaceId}/coordinates", GetCoordinatesAsync);
        spaces.MapGet("{spaceId}/index", IndexAsync);
        spaces.MapGet("{spaceId}/discover", Discover);
        spaces.MapPost("{spaceId}", PostAsync);
        spaces.MapPut("{spaceId}", SaveSpaceAsync);
        spaces.MapPost("graph", async (BlossomSpaces spaces, ExtractGraphRequest request) =>
        {
            var result = await spaces.ExtractGraph(request);
            return Results.Ok(result);
        });

        // Map the presence endpoint
        endpoints.MapGet("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId) =>
        {
            return await GetPresenceAsync(principal, userId);
        });
        endpoints.MapPut("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId, BlossomPresence presence) =>
        {
            await SetPresenceAsync(principal, userId, presence);
            return Results.Ok();
        });
    }
}
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
    SparcAuthenticator<BlossomUser> auth)
    : BlossomAggregate<BlossomSpace>(options), IBlossomEndpoints
{
    public const string Domain = "sparc.coop";
    public string? MatrixSenderId;

    internal IQueryable<BlossomEvent> Query<T>()
    {
        var type = typeof(T).Name;
        return events.Query
            .Where(e => e.Type == type)
            .OrderBy(x => x.Depth);
    }

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

    private async Task<List<BlossomSpace>> GetSpacesAsync(string domain, string? rootSpaceId = null, int? limit = null)
    {
        var spaces = rootSpaceId == null
            ? await Repository.Query
            .Where(x => x.Domain == domain && x.RoomType == "Root")
            .ToListAsync()
            : await Repository.Query
            .Where(x => x.Domain == domain && x.ParentSpaceId == rootSpaceId)
            .ToListAsync();

        return spaces;
    }

    internal async Task<List<BlossomEvent>> GetAllAsync(string spaceId)
    {
        return await events.Query
            .Where(x => x.SpaceId == spaceId)
            .OrderBy(x => x.Depth)
            .ToListAsync();
    }

    internal async Task<List<BlossomEvent<T>>> GetAllAsync<T>(string spaceId)
    {
        var type = typeof(T).Name;
        var result = await events.Query
            .Where(e => e.SpaceId == spaceId && e.Type == type)
            .OrderBy(x => x.Depth)
            .ToListAsync();

        return result.Cast<BlossomEvent<T>>().ToList();
    }

    internal async Task<BlossomSpace> GetSpaceAsync(string domain, string spaceId)
    {
        var space = await Repository.FindAsync(domain, spaceId)
            ?? throw new InvalidOperationException($"Space '{spaceId}' not found in domain '{domain}'.");
        return space;
    }

    private async Task<BlossomSpace> GetOrCreate(string domain, string? spaceId)
    {
        if (string.IsNullOrWhiteSpace(spaceId))
            spaceId = Guid.NewGuid().ToString();

        var existing = await Repository.FindAsync(domain, spaceId);
        if (existing == null)
        {
            existing = new BlossomSpace(domain, spaceId);
            await Repository.AddAsync(existing);
        }

        return existing;
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
        var existing = await Repository.Query.Where(x => x.ParentSpaceId == spaceId).ToListAsync();
        await Repository.DeleteAsync(existing);

        var space = await GetOrCreate(Domain, spaceId);
        var spacePosts = await GetPostsAsync(spaceId, 10000);
        var answer = await faceter.AnswerAsync(space, spacePosts);
        //var spaces = await faceter.DivideAsync(space, spacePosts, 1M);
        var facets = await faceter.FacetAsync(space, spacePosts);
        //foreach (var childSpace in spaces)
        //    facets.AddRange(await faceter.FacetAsync(childSpace, spacePosts));

        await posts.UpdateAsync(spacePosts);
        await Repository.UpdateAsync(space);
        //await Repository.AddAsync(spaces);
        await Repository.AddAsync(facets);
        return facets;
    }

    private async Task<BlossomPost> PostAsync(string spaceId, BlossomPost post)
    {
        await GetOrCreate(Domain, post.SpaceId);
        await posts.AddAsync(post);
        await vectors.AddAsync(post);
        await Discover(spaceId);
        return post;
    }

    private async Task<List<BlossomPost>> GetPostsAsync(string spaceId, int take = 50)
    {
        var space = await GetOrCreate(Domain, spaceId);
        var exactPosts = await posts.Query
                .Where(x => x.Domain == space.Domain &&
                (x.SpaceId == spaceId || 
                x.LinkedSpaces.Any(y => y.SpaceId == space.Id)))
                .OrderByDescending(x => x.Timestamp)
                .Take(take)
                .ToListAsync();

        return exactPosts;
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
        spaces.MapPost("{spaceId}/join", JoinSpaceAsync);
        spaces.MapPost("{spaceId}/leave", LeaveSpaceAsync);
        spaces.MapPost("{spaceId}/invite", InviteToSpaceAsync);
        spaces.MapGet("{spaceId}/posts", GetPostsAsync);
        spaces.MapGet("{spaceId}/simpleposts", GetSimplePostsAsync);
        spaces.MapGet("{spaceId}/index", IndexAsync);
        spaces.MapGet("{spaceId}/discover", Discover);
        spaces.MapPost("{spaceId}", PostAsync);
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
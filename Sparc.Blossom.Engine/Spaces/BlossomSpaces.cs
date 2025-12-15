using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaces(
    IRepository<BlossomEvent> events,
    IHttpContextAccessor http,
    Contents contents,
    SparcAuthenticator<BlossomUser> auth)
    : IBlossomEndpoints
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

    private async Task<GetPublicSpacesResponse> GetSpacesAsync(int? limit = null, string? since = null, string? server = null)
    {
        var createdSpaces = await Query<CreateSpace>().ToListAsync();

        var spaces = new List<BlossomSpace>();
        // Eventually do this in the background to show a published room directory
        foreach (var createdSpace in createdSpaces)
        {
            var space = await GetSpaceAsync(createdSpace.SpaceId);
            spaces.Add(space);
        }

        return new(spaces);
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

    internal async Task<BlossomSpace> GetSpaceAsync(string spaceId)
    {
        var allSpaceEvents = await GetAllAsync(spaceId);
        var orderedEvents = allSpaceEvents.OrderBy(x => x.Depth);

        var rootEvent = orderedEvents.OfType<BlossomEvent<CreateSpace>>().First();

        var space = new BlossomSpace(Domain, rootEvent.SpaceId, rootEvent.Content.Type);
        return space;
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

    private async Task<BlossomPost> PostAsync(string spaceId, BlossomPost post)
    {
        return post;
    }

    private async Task<List<BlossomEvent<MatrixMessage>>> GetPostsAsync(string spaceId)
    {
        return await GetAllAsync<MatrixMessage>(spaceId);
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

    private async Task IndexAsync()
    {
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var spaces = endpoints.MapGroup("/spaces");

        endpoints.MapGet("", GetSpacesAsync);
        endpoints.MapPost("", CreateSpaceAsync);
        endpoints.MapPost("{spaceId}/join", JoinSpaceAsync);
        endpoints.MapPost("{spaceId}/leave", LeaveSpaceAsync);
        endpoints.MapPost("{spaceId}/invite", InviteToSpaceAsync);
        endpoints.MapGet("{spaceId}/posts", GetPostsAsync);
        endpoints.MapPost("{spaceId}", PostAsync);
        endpoints.MapPost("{spaceId}/index", IndexAsync);
        endpoints.MapPost("graph", async (BlossomSpaces spaces, ExtractGraphRequest request) =>
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
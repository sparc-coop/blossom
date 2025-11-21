using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class BlossomSpaces(
    IRepository<BlossomEvent> events,
    IHttpContextAccessor http,
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

    private async Task<GetPublicRoomsResponse> GetRoomsAsync(int? limit = null, string? since = null, string? server = null)
    {
        var createdRooms = await Query<CreateRoom>().ToListAsync();

        var rooms = new List<MatrixRoom>();
        // Eventually do this in the background to show a published room directory
        foreach (var createdRoom in createdRooms)
        {
            var room = await GetRoomAsync(createdRoom.RoomId);
            rooms.Add(room);
        }

        return new(rooms);
    }

    private async Task<MatrixRoom> GetRoomSummaryAsync(string roomId)
    {
        if (!roomId.Contains(':'))
            roomId = roomId + ":" + Domain;

        return await GetRoomAsync(roomId);
    }

    internal async Task<List<BlossomEvent>> GetAllAsync(string roomId)
    {
        return await events.Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.Depth)
            .ToListAsync();
    }

    internal async Task<List<BlossomEvent<T>>> GetAllAsync<T>(string roomId)
    {
        var type = typeof(T).Name;
        var result = await events.Query
            .Where(e => e.RoomId == roomId && e.Type == type)
            .OrderBy(x => x.Depth)
            .ToListAsync();

        return result.Cast<BlossomEvent<T>>().ToList();
    }

    internal async Task<MatrixRoom> GetRoomAsync(string roomId)
    {
        var allRoomEvents = await GetAllAsync(roomId);
        return MatrixRoom.From(allRoomEvents);
    }

    private async Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var roomId = "!" + BlossomEvent.OpaqueId() + ":" + Domain;
        await PublishAsync(roomId, new CreateRoom());
        await PublishAsync(roomId, new ChangeMembershipState("join", MatrixSenderId!));
        await PublishAsync(roomId, new AdjustPowerLevels());

        if (!string.IsNullOrWhiteSpace(request.RoomAliasName))
            await PublishAsync(roomId, new CanonicalAlias(request.RoomAliasName));

        if (!string.IsNullOrWhiteSpace(request.Preset))
        {
            switch (request.Preset)
            {
                case "public_chat":
                    await PublishAsync(roomId, new JoinRules("public"));
                    await PublishAsync(roomId, HistoryVisibility.Shared);
                    await PublishAsync(roomId, GuestAccess.Forbidden);
                    break;
                case "private_chat":
                case "trusted_private_chat":
                    await PublishAsync(roomId, new JoinRules("invite"));
                    await PublishAsync(roomId, HistoryVisibility.Shared);
                    await PublishAsync(roomId, GuestAccess.CanJoin);
                    break;
                default:
                    throw new NotSupportedException($"Preset '{request.Preset}' is not supported.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            await PublishAsync(roomId, new RoomName(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Topic))
            await PublishAsync(roomId, new RoomTopic(request.Topic));

        if (request.Invite?.Count > 0)
        {
            foreach (var userId in request.Invite)
                await PublishAsync(roomId, new ChangeMembershipState("invite", userId));
        }

        return new(roomId);
    }

    private async Task JoinRoomAsync(string roomId)
    {
        await PublishAsync(roomId, new ChangeMembershipState("join", MatrixSenderId!));
    }

    private async Task LeaveRoomAsync(string roomId)
    {
        await PublishAsync(roomId, new ChangeMembershipState("leave", MatrixSenderId!));
    }

    private async Task InviteToRoomAsync(string roomId, InviteToRoomRequest request)
    {
        await PublishAsync(roomId, new ChangeMembershipState("invite", request.UserId));
    }

    private async Task<SendMessageReponse> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request)
    {
        var ev = await PublishAsync(roomId, new MatrixMessage(request.Body));
        return new(ev.EventId);
    }

    private async Task<List<BlossomEvent<MatrixMessage>>> GetMessagesAsync(string roomId)
    {
        return await GetAllAsync<MatrixMessage>(roomId);
    }

    private async Task<BlossomEvent> PublishAsync<T>(string roomId, T content)
    {
        var sender = await GetMatrixSenderIdAsync();

        var ev = BlossomEvent.Create(roomId, sender, content);
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

    //private async Task<GetSyncResponse> GetSyncAsync()
    //{
    //    var user = await auth.GetAsync(principal);
    //    return await events.GetSyncAsync(user, since, timeout);
    //}

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/_matrix/client/v3");

        //chatGroup.MapGet("/sync", GetSyncAsync);
        chatGroup.MapGet("/publicRooms", GetRoomsAsync);
        chatGroup.MapPost("/createRoom", CreateRoomAsync);
        chatGroup.MapPost("/join/{roomId}", JoinRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/leave", LeaveRoomAsync);
        chatGroup.MapPost("/rooms/{roomId}/invite", InviteToRoomAsync);
        chatGroup.MapGet("/rooms/{roomId}/messages", GetMessagesAsync);
        chatGroup.MapPost("/rooms/{roomId}/send/{eventType}/{txnId}", SendMessageAsync);

        // Map the presence endpoint
        chatGroup.MapGet("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId) =>
        {
            return await GetPresenceAsync(principal, userId);
        });
        chatGroup.MapPut("/presence/{userId}/status", async (ClaimsPrincipal principal, string userId, BlossomPresence presence) =>
        {
            await SetPresenceAsync(principal, userId, presence);
            return Results.Ok();
        });

        var legacyChatGroup = endpoints.MapGroup("/_matrix/client/v1");
        legacyChatGroup.MapGet("/room_summary/{roomId}", GetRoomSummaryAsync);
    }
}
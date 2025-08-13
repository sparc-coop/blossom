using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime.Matrix;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class MatrixChats(ClaimsPrincipal principal, MatrixEvents events, SparcAuthenticator<BlossomUser> auth)
    : IBlossomEndpoints, ISparcChat
{
    public async Task<BlossomPresence> GetPresenceAsync(string userId)
    {
        var user = await auth.GetAsync(principal);
        return new BlossomPresence(user.Avatar);
    }

    public async Task SetPresenceAsync(string userId, BlossomPresence presence)
    {
        var user = await auth.GetAsync(principal);
        presence.ApplyToAvatar(user.Avatar);
        user.UpdateAvatar(user.Avatar);
        await auth.UpdateAsync(user);
    }

    public async Task<GetPublicRoomsResponse> GetPublicRoomsAsync(int? limit = null, string? since = null, string? server = null)
    {
        var createdRooms = await events.Query<CreateRoom>().ToListAsync();

        var rooms = new List<MatrixRoomSummary>();
        // Eventually do this in the background to show a published room directory
        foreach (var createdRoom in createdRooms)
        {
            var room = await events.GetRoomAsync(createdRoom.RoomId);
            rooms.Add(room);
        }

        return new(rooms);
    }

    public async Task<MatrixRoomSummary> GetRoomSummaryAsync(string roomId)
    {
        if (!roomId.Contains(':'))
            roomId = roomId + ":" + MatrixEvents.Domain;

        return await events.GetRoomAsync(roomId);
    }

    public async Task<RoomIdResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var roomId = "!" + MatrixEvent.OpaqueId() + ":" + MatrixEvents.Domain;
        await events.PublishAsync(roomId, new CreateRoom());
        await events.PublishAsync(roomId, new ChangeMembershipState("join", events.MatrixSenderId!));
        await events.PublishAsync(roomId, new AdjustPowerLevels());

        if (!string.IsNullOrWhiteSpace(request.RoomAliasName))
            await events.PublishAsync(roomId, new CanonicalAlias(request.RoomAliasName));

        if (!string.IsNullOrWhiteSpace(request.Preset))
        {
            switch (request.Preset)
            {
                case "public_chat":
                    await events.PublishAsync(roomId, new JoinRules("public"));
                    await events.PublishAsync(roomId, HistoryVisibility.Shared);
                    await events.PublishAsync(roomId, GuestAccess.Forbidden);
                    break;
                case "private_chat":
                case "trusted_private_chat":
                    await events.PublishAsync(roomId, new JoinRules("invite"));
                    await events.PublishAsync(roomId, HistoryVisibility.Shared);
                    await events.PublishAsync(roomId, GuestAccess.CanJoin);
                    break;
                default:
                    throw new NotSupportedException($"Preset '{request.Preset}' is not supported.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            await events.PublishAsync(roomId, new RoomName(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Topic))
            await events.PublishAsync(roomId, new RoomTopic(request.Topic));

        if (request.Invite?.Count > 0)
        {
            foreach (var userId in request.Invite)
                await events.PublishAsync(roomId, new ChangeMembershipState("invite", userId));
        }

        return new(roomId);
    }

    public async Task<RoomIdResponse> JoinRoomAsync(string roomId)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("join", events.MatrixSenderId!));
        return new(roomId);
    }

    public async Task<EmptyResponse> LeaveRoomAsync(string roomId)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("leave", events.MatrixSenderId!));
        return new();
    }

    public async Task<EmptyResponse> InviteToRoomAsync(string roomId, InviteToRoomRequest request)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("invite", request.UserId));
        return new();
    }

    public async Task<SendMessageReponse> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request)
    {
        var ev = await events.PublishAsync(roomId, new MatrixMessage(request.Body));
        return new(ev.EventId);
    }

    public async Task<List<MatrixEvent<MatrixMessage>>> GetMessagesAsync(string roomId)
    {
        return await events.GetAllAsync<MatrixMessage>(roomId);
    }

    public async Task<GetSyncResponse> SyncAsync(string? Since = null, string? Filter = null, bool FullState = false, string? SetPresence = null, int Timeout = 0)
    {
        var since = long.Parse(Since ?? "0");

        var roomUpdates = await events.GetRoomUpdatesAsync(since);

        var lastTimestamp = roomUpdates.Join.Count > 0 
            ? roomUpdates.Join.Values.Max(x => x.Timeline.Events.Max(y => y.OriginServerTs)).ToString() 
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        return new(lastTimestamp, roomUpdates, new([]), new([]));
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/_matrix/client/v3");

        //chatGroup.MapGet("/sync", GetSyncAsync);
        chatGroup.MapGet("/publicRooms", async (MatrixChats chats) => await chats.GetPublicRoomsAsync());
        chatGroup.MapPost("/createRoom", async (MatrixChats chats, CreateRoomRequest request) => await chats.CreateRoomAsync(request));
        chatGroup.MapPost("/join/{roomId}", async (MatrixChats chats, string roomId) => await chats.JoinRoomAsync(roomId));
        chatGroup.MapPost("/rooms/{roomId}/leave", async (MatrixChats chats, string roomId) => await chats.LeaveRoomAsync(roomId));
        chatGroup.MapPost("/rooms/{roomId}/invite", async (MatrixChats chats, string roomId, InviteToRoomRequest request) => await chats.InviteToRoomAsync(roomId, request));
        chatGroup.MapGet("/rooms/{roomId}/messages", async (MatrixChats chats, string roomId) => await chats.GetMessagesAsync(roomId));
        chatGroup.MapPost("/rooms/{roomId}/send/{eventType}/{txnId}", SendMessageAsync);
        chatGroup.MapGet("/sync", async (MatrixChats chats, string? since, string? filter, bool fullState, string? setPresence, int timeout) =>
            await chats.SyncAsync(since, filter, fullState, setPresence, timeout));

        // Map the presence endpoint
        chatGroup.MapGet("/presence/{userId}/status", async (MatrixChats chats, string userId) => await chats.GetPresenceAsync(userId));
        chatGroup.MapPut("/presence/{userId}/status", async (MatrixChats chats, string userId, BlossomPresence presence) =>
        {
            await chats.SetPresenceAsync(userId, presence);
            return Results.Ok();
        });

        var legacyChatGroup = endpoints.MapGroup("/_matrix/client/v1");
        legacyChatGroup.MapGet("/room_summary/{roomId}", GetRoomSummaryAsync);
    }
}
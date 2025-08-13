using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime.Matrix;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class SparcEngineChatService(MatrixEvents events, SparcAuthenticator<BlossomUser> auth)
    : IBlossomEndpoints
{
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
    }

    private async Task<GetPublicRoomsResponse> GetRoomsAsync(int? limit = null, string? since = null, string? server = null)
    {
        var createdRooms = await events.Query<CreateRoom>().ToListAsync();

        var rooms = new List<MatrixRoom>();
        // Eventually do this in the background to show a published room directory
        foreach (var createdRoom in createdRooms)
        {
            var room = await events.GetRoomAsync(createdRoom.RoomId);
            rooms.Add(room);
        }

        return new(rooms);
    }

    private async Task<MatrixRoom> GetRoomSummaryAsync(string roomId)
    {
        if (!roomId.Contains(':'))
            roomId = roomId + ":" + MatrixEvents.Domain;

        return await events.GetRoomAsync(roomId);
    }

    private async Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request)
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

    private async Task<MatrixRoom> DeleteRoomAsync(string roomId)
    {
        //var memberships = await Memberships.Query
        //    .Where(m => m.RoomId == roomId)
        //    .ToListAsync();

        //if (memberships != null)
        //{
        //    foreach (var membership in memberships)
        //    {
        //        await Memberships.DeleteAsync(membership);
        //    }
        //}

        //var room = await Rooms.FindAsync(roomId);
        //if (room != null)
        //    await Rooms.DeleteAsync(room);

        //return room;
        return null;
    }

    private async Task JoinRoomAsync(string roomId)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("join", events.MatrixSenderId!));
    }

    private async Task LeaveRoomAsync(string roomId)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("leave", events.MatrixSenderId!));
    }

    private async Task InviteToRoomAsync(string roomId, InviteToRoomRequest request)
    {
        await events.PublishAsync(roomId, new ChangeMembershipState("invite", request.UserId));
    }

    private async Task<SendMessageReponse> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request)
    {
        var ev = await events.PublishAsync(roomId, new MatrixMessage(request.Body));
        return new(ev.EventId);
    }

    private async Task<List<MatrixEvent<MatrixMessage>>> GetMessagesAsync(string roomId)
    {
        return await events.GetAllAsync<MatrixMessage>(roomId);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var chatGroup = endpoints.MapGroup("/_matrix/client/v3");

        chatGroup.MapGet("/publicRooms", GetRoomsAsync);
        chatGroup.MapPost("/createRoom", CreateRoomAsync);
        chatGroup.MapPost("/deleteRoom/{roomId}", DeleteRoomAsync);
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
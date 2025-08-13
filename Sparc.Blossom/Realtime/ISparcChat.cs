using Refit;
using Sparc.Blossom.Realtime.Matrix;

namespace Sparc.Blossom.Realtime;

public interface ISparcChat
{
    [Get("/_matrix/client/v3/publicRooms")]
    Task<GetPublicRoomsResponse> GetRoomsAsync(int? limit = null, string? since = null, string? server = null);

    [Get("/_matrix/client/v1/room_summary/{roomId}")]
    Task<MatrixRoom> GetRoomSummaryAsync(string roomId);

    [Post("/_matrix/client/v3/createRoom")]
    Task<MatrixRoom> CreateRoomAsync(CreateRoomRequest request);

    [Post("/_matrix/client/v3/deleteRoom/{roomId}")]
    Task<MatrixRoom> DeleteRoomAsync(string roomId);

    [Post("/_matrix/client/v3/join/{roomId}")]
    Task<MatrixRoom> JoinRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/leave")]
    Task<MatrixRoom> LeaveRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/invite")]
    Task<MatrixRoom> InviteToRoomAsync(string roomId, InviteToRoomRequest request);

    [Get("/_matrix/client/v3/rooms/{roomId}/messages")]
    Task<List<MatrixEvent<MatrixMessage>>> GetMessagesAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/send/{eventType}/{txnId}")]
    Task<MatrixEvent> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request);

    [Get("/_matrix/client/v3/presence/{userId}/status")]
    Task<BlossomPresence> GetPresenceAsync(string userId);

    [Put("/_matrix/client/v3/presence/{userId}/status")]
    Task SetPresenceAsync(string userId, BlossomPresence presence);
}

public record InviteToRoomRequest(string UserId);
public record CreateRoomRequest(
    string? Name = null,
    string Visibility = "private",
    string? Topic = null,
    string RoomVersion = "1",
    string? RoomAliasName = null,
    string? Preset = null,
    bool? IsDirect = null,
    List<string>? Invite = null,
    List<StateEvent>? InitialState = null,
    Dictionary<string, object>? CreationContent = null);
public record CreateRoomResponse(string RoomId);
public record SendMessageRequest(string Body, string MsgType = "m.text");
public record DeleteRoomRequest(string RoomId);

public record GetSyncResponse(
    string NextBatch,
    Rooms Rooms,
    EventList AccountData,
    EventList Presence,
    EventList ToDevice,
    Dictionary<string, int>? DeviceOneTimeKeysCount = null,
    DeviceLists? DeviceLists = null);

public record Rooms(
    Dictionary<string, JoinedRoom> Join,
    Dictionary<string, InvitedRoom> Invite,
    Dictionary<string, KnockedRoom> Knock,
    Dictionary<string, LeftRoom> Leave);

public record EventList(List<MatrixEvent> Events);
public record DeviceLists(List<string> Changed, List<string> Left);

public record JoinedRoom(
    EventList AccountData,
    EventList Ephemeral,
    EventList State,
    RoomSummary Summary,
    Timeline Timeline,
    NotificationCounts UnreadNotifications,
    Dictionary<string, NotificationCounts>? UnreadThreadNotifications);

public record InvitedRoom(EventList InviteState);
public record KnockedRoom(EventList KnockState);
public record LeftRoom(EventList AccountData, EventList State, EventList Timeline);

public record RoomSummary(List<string> Heroes, int InvitedMemberCount, int JoinedMemberCount);
public record Timeline(List<MatrixEvent> Events, bool Limited, string? PrevBatch);
public record NotificationCounts(int NotificationCount, int HighlightCount);

public record RoomFilter(RoomEventFilter? Room, RoomEventFilter? RoomState);
public record RoomEventFilter(int? Limit = null, bool? LazyLoadMembers = null);
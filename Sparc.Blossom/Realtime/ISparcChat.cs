using Refit;
using Sparc.Blossom.Realtime.Matrix;

namespace Sparc.Blossom.Realtime;

public interface ISparcChat
{
    [Get("/_matrix/client/v3/publicRooms")]
    Task<GetPublicRoomsResponse> GetPublicRoomsAsync(int? limit = null, string? since = null, string? server = null);

    [Get("/_matrix/client/v1/room_summary/{roomId}")]
    Task<MatrixRoomSummary> GetRoomSummaryAsync(string roomId);

    [Post("/_matrix/client/v3/createRoom")]
    Task<RoomIdResponse> CreateRoomAsync(CreateRoomRequest request);

    [Post("/_matrix/client/v3/join/{roomId}")]
    Task<RoomIdResponse> JoinRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/leave")]
    Task<EmptyResponse> LeaveRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/invite")]
    Task<EmptyResponse> InviteToRoomAsync(string roomId, InviteToRoomRequest request);

    [Get("/_matrix/client/v3/rooms/{roomId}/messages")]
    Task<List<MatrixEvent<MatrixMessage>>> GetMessagesAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/send/{eventType}/{txnId}")]
    Task<SendMessageReponse> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request);

    [Get("/_matrix/client/v3/presence/{userId}/status")]
    Task<BlossomPresence> GetPresenceAsync(string userId);

    [Put("/_matrix/client/v3/presence/{userId}/status")]
    Task SetPresenceAsync(string userId, BlossomPresence presence);

    [Get("/_matrix/client/v3/sync")]
    Task<GetSyncResponse> SyncAsync(string? Since = null,
        string? Filter = null,
        bool FullState = false,
        string? SetPresence = null,
        int Timeout = 0);
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
public record EmptyResponse();
public record RoomIdResponse(string RoomId);
public record SendMessageRequest(string Body, string MsgType = "m.text");
public record DeleteRoomRequest(string RoomId);

public record GetSyncResponse(
    string NextBatch,
    Rooms Rooms,
    EventList AccountData,
    EventList Presence,
    EventList? ToDevice = null,
    Dictionary<string, int>? DeviceOneTimeKeysCount = null,
    DeviceLists? DeviceLists = null);

public record Rooms(
    Dictionary<string, JoinedRoom> Join,
    Dictionary<string, InvitedRoom> Invite,
    Dictionary<string, KnockedRoom> Knock,
    Dictionary<string, LeftRoom> Leave);

public record EventList(List<MatrixEvent> Events);
public record StrippedStateEventList(List<MatrixStrippedStateEvent> Events);
public record DeviceLists(List<string> Changed, List<string> Left);

public record JoinedRoom(
    EventList AccountData,
    EventList Ephemeral,
    EventList State,
    RoomSummary Summary,
    Timeline Timeline,
    NotificationCounts UnreadNotifications,
    Dictionary<string, NotificationCounts>? UnreadThreadNotifications)
{
    public JoinedRoom() : this(
        new EventList([]),
        new EventList([]),
        new EventList([]),
        new RoomSummary([], 0, 0),
        new Timeline([], false, null),
        new NotificationCounts(0, 0),
        null)
    { }
}

public record InvitedRoom(StrippedStateEventList InviteState);
public record KnockedRoom(StrippedStateEventList KnockState);
public record LeftRoom(EventList AccountData, EventList State, EventList Timeline);

public record RoomSummary(List<string> Heroes, int InvitedMemberCount, int JoinedMemberCount);
public record Timeline(List<MatrixEvent> Events, bool Limited, string? PrevBatch);
public record NotificationCounts(int NotificationCount, int HighlightCount);

public record RoomFilter(RoomEventFilter? Room, RoomEventFilter? RoomState);
public record RoomEventFilter(int? Limit = null, bool? LazyLoadMembers = null);
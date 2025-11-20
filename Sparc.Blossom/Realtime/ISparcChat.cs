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
    Task<List<Matrix.BlossomEvent<MatrixMessage>>> GetMessagesAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/send/{eventType}/{txnId}")]
    Task<BlossomEvent> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request);

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
    string next_batch,
    Rooms rooms,
    AccountData account_data,
    Presence presence,
    ToDevice to_device,
    Dictionary<string, int> device_one_time_keys_count,
    DeviceLists device_lists);

public record Rooms(
    Dictionary<string, JoinedRoom> join,
    Dictionary<string, InvitedRoom> invite,
    Dictionary<string, KnockedRoom> knock,
    Dictionary<string, LeftRoom> leave);

public record AccountData(List<ClientEvent> events);
public record Presence(List<ClientEvent> events);
public record ToDevice(List<ClientEvent> events);
public record DeviceLists(List<string> changed, List<string> left);

public record JoinedRoom(
    AccountData account_data,
    Ephemeral ephemeral,
    State state,
    RoomSummary summary,
    Timeline timeline,
    UnreadNotificationCounts unread_notifications,
    Dictionary<string, ThreadNotificationCounts>? unread_thread_notifications);

public record InvitedRoom(InviteState invite_state);
public record KnockedRoom(KnockState knock_state);
public record LeftRoom(AccountData account_data, State state, Timeline timeline);

public record InviteState(List<StrippedStateEvent> events);
public record KnockState(List<StrippedStateEvent> events);

public record Ephemeral(List<ClientEvent> events);
public record State(List<ClientEventWithState> events);
public record RoomSummary(Dictionary<string, object>? dummy = null);
public record Timeline(List<ClientEventWithState> events, bool limited, string? prev_batch);
public record UnreadNotificationCounts(int notification_count, int highlight_count);
public record ThreadNotificationCounts(int notification_count, int highlight_count);

public record ClientEvent(string type, object content, string? sender = null);
public record ClientEventWithState(string type, object content, string sender, string? state_key = null);

public record StrippedStateEvent(string type, string state_key, string sender, object content);
public record RoomFilter(RoomEventFilter? room, RoomEventFilter? room_state);
public record RoomEventFilter(int? limit = null, bool? lazy_load_members = null);
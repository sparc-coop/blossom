using Refit;
using Sparc.Blossom.Content;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Spaces;

public interface ISparcSpaces
{
    [Get("/spaces")]
    Task<List<BlossomSpace>> GetSpacesAsync(string? parentSpaceId = null, int? limit = null);

    [Get("/spaces/{spaceId}")]
    Task<BlossomSpace?> GetSpaceAsync(string spaceId);

    [Get("/spaces/{parentSpaceId}/subspaces/{spaceId}")]
    Task<BlossomSpace?> GetSpaceAsync(string parentSpaceId, string spaceId);

    [Post("/spaces")]
    Task<BlossomSpace> CreateSpaceAsync(CreateSpaceRequest request);

    [Post("/spaces/{spaceId}")]
    Task<BlossomPost> PostAsync(string spaceId, BlossomPost post);

    [Put("/spaces/{spaceId}")]
    Task SaveSpaceAsync(string spaceId, BlossomSpace space);

    [Delete("/spaces/{spaceId}")]
    Task<BlossomSpace> DeleteSpaceAsync(string spaceId);

    [Post("/spaces/{spaceId}/join")]
    Task<BlossomSpace> JoinSpaceAsync(string spaceId);

    [Post("/spaces/{spaceId}/leave")]
    Task<BlossomSpace> LeaveSpaceAsync(string spaceId);

    [Post("/spaces/{spaceId}/invite")]
    Task<BlossomSpace> InviteToSpaceAsync(string spaceId, InviteToSpaceRequest request);

    [Get("/spaces/{spaceId}/posts")]
    Task<List<BlossomPost>> GetPostsAsync(string spaceId);

    [Get("/spaces/{spaceId}/coordinates")]
    Task<List<BlossomCoordinate>> GetCoordinatesAsync(string spaceId);

    [Post("/spaces/rooms/{spaceId}/send/{eventType}/{txnId}")]
    Task<BlossomEvent> SendMessageAsync(string spaceId, string eventType, string txnId, SendMessageRequest request);

    [Get("/spaces/presence/{userId}/status")]
    Task<BlossomPresence> GetPresenceAsync(string userId);

    [Put("/spaces/presence/{userId}/status")]
    Task SetPresenceAsync(string userId, BlossomPresence presence);
}

public record InviteToSpaceRequest(string UserId);
public record CreateSpaceRequest(
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
public record CreateSpaceResponse(string spaceId);
public record SendMessageRequest(string Body, string MsgType = "m.text");
public record DeleteSpaceRequest(string spaceId);

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
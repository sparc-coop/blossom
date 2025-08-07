using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Get("/_matrix/client/v3/publicRooms")]
    Task<GetPublicRoomsResponse> GetRoomsAsync(int? limit = null, string? since = null, string? server = null);

    [Post("/_matrix/client/v3/createRoom")]
    Task<MatrixRoom> CreateRoomAsync(CreateRoomRequest request);

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
    Task<MatrixPresence> GetPresenceAsync(string userId);

    [Put("/_matrix/client/v3/presence/{userId}/status")]
    Task SetPresenceAsync(string userId, MatrixPresence presence);
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
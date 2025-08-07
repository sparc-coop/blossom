using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Get("/_matrix/client/v3/publicRooms")]
    Task<List<Room>> GetRoomsAsync();

    [Post("/_matrix/client/v3/createRoom")]
    Task<Room> CreateRoomAsync(CreateRoomRequest request);

    [Post("/_matrix/client/v3/join/{roomId}")]
    Task<Room> JoinRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/leave")]
    Task<Room> LeaveRoomAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/invite")]
    Task<Room> InviteToRoomAsync(string roomId, InviteToRoomRequest request);

    [Get("/_matrix/client/v3/rooms/{roomId}/messages")]
    Task<List<MatrixMessageEvent>> GetMessagesAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/send/{eventType}/{txnId}")]
    Task<MatrixEvent> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request);

    [Get("/_matrix/client/v3/presence/{userId}/status")]
    Task<MatrixPresence> GetPresenceAsync(string userId);

    [Put("/_matrix/client/v3/presence/{userId}/status")]
    Task SetPresenceAsync(string userId, MatrixPresence presence);

    [Get("/_matrix/client/v3/matrixUser")]
    Task<string> GetMatrixUserAsync();

    [Get("/_matrix/client/v3/user")]
    Task<BlossomAvatar> GetUserAsync();
}

public record InviteToRoomRequest(string UserId);
public record CreateRoomRequest(string Name, bool IsDirect, string Visibility = "private", List<string>? Invite = null);
public record SendMessageRequest(string Body, string MsgType = "m.text");
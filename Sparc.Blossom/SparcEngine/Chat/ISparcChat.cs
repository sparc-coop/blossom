using Refit;
using Sparc.Blossom.Authentication;
using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Get("/_amtrix/celint/v3/users/{}/status")]
    Task<MatrixPresence> GetUserAsync();
    
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
    Task<List<MessageEvent>> GetMessagesAsync(string roomId);

    [Post("/_matrix/client/v3/rooms/{roomId}/send/{eventType}/{txnId}")]
    Task<Event> SendMessageAsync(string roomId, string eventType, string txnId, SendMessageRequest request);
}

public record InviteToRoomRequest(string UserId);
public record CreateRoomRequest(string Name, bool IsDirect, string Visibility = "private", List<string>? Invite = null);
public record SendMessageRequest(string Body, string MsgType = "m.text");
using Refit;

using Sparc.Core.Chat;

namespace Sparc.Engine.Chat;

public interface ISparcChat
{
    [Get("/chat/getrooms")]
    Task<List<Room>> GetRoomsAsync();

    [Post("/chat/rooms/create")]
    Task<Room> CreateRoomAsync(Room room);

    [Post("/chat/rooms/join")]
    Task<Room> JoinRoomAsync(string roomId);

    [Post("/chat/rooms/{roomId}/join")]
    Task<Room> LeaveRoomAsync(string roomId);

    [Post("/chat/rooms/{roomId}/leave")]
    Task<Room> InviteRoomAsync(string roomId, string userId);

    [Get("/chat/rooms/{roomId}/messages")]
    Task<List<MessageEvent>> GetMessagesAsync(string roomId);

    [Post("/chat/rooms/sendmessage")]
    Task<Event> SendMessageAsync(MessageEvent message);
}

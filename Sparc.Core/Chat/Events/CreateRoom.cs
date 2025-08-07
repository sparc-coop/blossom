namespace Sparc.Core.Chat;

public record PreviousRoom(string RoomId, string EventId);
public record CreateRoom(
    string RoomVersion = "1",
    bool Federate = true,
    string? Type = null,
    PreviousRoom? PreviousRoom = null);
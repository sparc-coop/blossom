namespace Sparc.Blossom.Realtime;

public record PreviousRoom(string RoomId, string EventId);
public record CreateSpace(
    string RoomVersion = "1",
    bool Federate = true,
    string? Type = null,
    PreviousRoom? PreviousRoom = null);
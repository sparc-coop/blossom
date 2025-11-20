namespace Sparc.Blossom.Realtime;

public enum HistoryVisibility
{
    Invited,
    Joined,
    Shared,
    WorldReadable
}

public enum GuestAccess
{
    CanJoin,
    Forbidden
}

public record StateEvent(string Type, object Content, string StateKey = "");

public record GetPublicRoomsResponse(
    List<MatrixRoom> Chunk,
    string? NextBatch = null,
    string? PrevBatch = null,
    int? TotalRoomCountEstimate = null
);

public record SendMessageReponse(string EventId);
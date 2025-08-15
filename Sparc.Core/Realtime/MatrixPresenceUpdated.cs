using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

// I think this should be MatrixPresence (or MatrixPresenceUpdated),
// as it directly matches the Matrix spec
public class MatrixPresenceUpdated
{
    public string Presence { get; set; } = "offline"; // "online", "offline", "unavailable"
    public string? StatusMsg { get; set; }
    public bool CurrentlyActive { get; set; }
    public long? LastActiveAt { get; set; }
    public long? LastActiveAgo =>
        LastActiveAt.HasValue
            ? Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastActiveAt.Value)
            : null;
}
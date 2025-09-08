namespace Sparc.Blossom.Realtime;

public class BlossomPresence
{
    public string Presence { get; set; } = "offline";
    public string? StatusMsg { get; set; }
    public bool CurrentlyActive { get; set; }
    public long? LastActiveAt { get; set; }

    public long? LastActiveAgo =>
        LastActiveAt.HasValue
            ? Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastActiveAt.Value)
            : null;

    public void UpdateFromMatrix(MatrixPresenceUpdated matrixPresence, bool isProactiveEvent)
    {
        if (matrixPresence == null) return;

        Presence = matrixPresence.Presence;
        StatusMsg = matrixPresence.StatusMsg;
        CurrentlyActive = matrixPresence.CurrentlyActive || Presence == "online"; ;

        if (isProactiveEvent || matrixPresence.LastActiveAt.HasValue)
            LastActiveAt = matrixPresence.LastActiveAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}


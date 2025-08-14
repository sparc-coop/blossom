using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Realtime;

// I think this should be MatrixPresence (or MatrixPresenceUpdated),
// as it directly matches the Matrix spec
public class BlossomPresence
{
    public string Presence { get; set; } = "offline"; // "online", "offline", "unavailable"
    public string? StatusMsg { get; set; }
    public bool CurrentlyActive { get; set; }
    public long? LastActiveAt { get; set; }
    public long? LastActiveAgo =>
        LastActiveAt.HasValue
            ? Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastActiveAt.Value)
            : null;
    

    public BlossomPresence() { }

    public BlossomPresence(BlossomAvatar avatar, bool isProactiveEvent = false)
    {
        if (avatar?.Presence != null)
        {
            Presence = avatar.Presence.Presence;
            StatusMsg = avatar.Presence.StatusMsg;

            CurrentlyActive = Presence == "online";

            if (isProactiveEvent)
                LastActiveAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            else
                LastActiveAt = avatar.Presence.LastActiveAt;
        }
        else
        {
            Presence = "offline";
            CurrentlyActive = false;
            LastActiveAt = null;
        }
    }

    public void ApplyToAvatar(BlossomAvatar avatar, bool isProactiveEvent = false)
    {
        if (avatar != null)
        {
            if (isProactiveEvent)
                LastActiveAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            CurrentlyActive = Presence == "online";

            avatar.Presence = new BlossomPresence
            {
                Presence = this.Presence,
                StatusMsg = this.StatusMsg,
                LastActiveAt = this.LastActiveAt,
                CurrentlyActive = this.CurrentlyActive
            };
        }
    }
}
using Sparc.Blossom.Authentication;

namespace Sparc.Engine.Chat;

public class MatrixPresence // converts from avatar
{
    public string Presence { get; set; } = "offline"; // "online", "offline", "unavailable"
    public string? StatusMsg { get; set; }
    public long? LastActiveAgo { get; set; }
    public bool? CurrentlyActive { get; set; }

    public MatrixPresence() { }

    public MatrixPresence(BlossomAvatar avatar)
    {
        Presence = avatar.Presence?.ToLowerInvariant() ?? "offline";
        StatusMsg = avatar.StatusMsg;

        if (avatar.LastActiveAt.HasValue)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LastActiveAgo = now - avatar.LastActiveAt.Value;
            CurrentlyActive = LastActiveAgo < 300_000;
        }
        else
        {
            CurrentlyActive = false;
        }
    }

    public BlossomAvatar ToAvatar()
    {
        return new BlossomAvatar
        {
            Presence = Presence,
            StatusMsg = StatusMsg,
            LastActiveAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
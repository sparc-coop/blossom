using Sparc.Blossom.Authentication;

namespace Sparc.Engine.Chat;

public class MatrixPresence // converts from avatar
{
    public string Presence { get; set; } = "offline"; // "online", "offline", "unavailable"
    public string? StatusMsg { get; set; }
    public long? LastActiveAgo { get; set; }
    public bool? CurrentlyActive { get; set; } // milliseconds

    //public string? AvatarUrl { get; set; } 
    //public string? DisplayName { get; set; }

    public MatrixPresence() { }

    public MatrixPresence(BlossomAvatar avatar)
    {
        if (avatar?.Presence != null)
        {
            Presence = avatar.Presence.Presence;
            StatusMsg = avatar.Presence.StatusMsg;
            LastActiveAgo = avatar.Presence.LastActiveAgo;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (LastActiveAgo.HasValue)
                CurrentlyActive = (now - LastActiveAgo.Value) < 300_000; // 5 minutes
            else
                CurrentlyActive = false;
        }
        else
        {
            Presence = "offline";
            CurrentlyActive = false;
        }
    }

    public void UpdateFromAvatar(BlossomAvatar avatar)
    {
        if (avatar?.Presence != null)
        {
            Presence = avatar.Presence.Presence;
            StatusMsg = avatar.Presence.StatusMsg;
            LastActiveAgo = avatar.Presence.LastActiveAgo;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            CurrentlyActive = LastActiveAgo.HasValue && (now - LastActiveAgo.Value) < 300_000;
        }
    }

    public void ApplyToAvatar(BlossomAvatar avatar)
    {
        if (avatar != null)
        {
            avatar.Presence = new MatrixPresence
            {
                Presence = this.Presence,
                StatusMsg = this.StatusMsg,
                LastActiveAgo = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                CurrentlyActive = this.CurrentlyActive
            };
        }
    }
}
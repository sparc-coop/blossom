namespace Sparc.Blossom.Realtime.Matrix;

public record AdjustPowerLevels(
    int Ban = 50,
    int EventsDefault = 0,
    int Invite = 0,
    int Kick = 50,
    int Redact = 50,
    int StateDefault = 50,
    int UsersDefault = 0,
    Dictionary<string, int>? Events = null,
    Dictionary<string, int>? Notifications = null,
    Dictionary<string, int>? Users = null
);
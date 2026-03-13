namespace Sparc.Blossom.Realtime;

public record ChangeMembershipState(
    string Membership,
    string StateKey,
    string? AvatarUrl = null,
    string? DisplayName = null,
    string? Reason = null,
    bool? IsDirect = null)
{
    //public void ApplyTo(MatrixRoom room)
    //{
    //    switch (Membership)
    //    {
    //        case "join":
    //            room.NumJoinedMembers++;
    //            break;
    //        case "leave":
    //            room.NumJoinedMembers--; 
    //            break; 
    //    }
    //}
}
namespace Sparc.Core.Chat;

public record RoomName(string Name) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoom room)
    {
        room.Name = Name;
    }
}
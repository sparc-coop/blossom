namespace Sparc.Blossom.Realtime.Matrix;

public record RoomName(string Name) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoom room)
    {
        room.Name = Name;
    }
}
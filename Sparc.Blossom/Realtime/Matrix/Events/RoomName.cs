namespace Sparc.Blossom.Realtime.Matrix;

public record RoomName(string Name) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoomSummary room)
    {
        room.Name = Name;
    }
}
namespace Sparc.Blossom.Realtime.Matrix;

public record RoomTopic(string Topic) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoom room)
    {
        room.Topic = Topic;
    }
}
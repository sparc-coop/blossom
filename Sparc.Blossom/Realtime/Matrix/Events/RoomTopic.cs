namespace Sparc.Blossom.Realtime.Matrix;

public record RoomTopic(string Topic) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoomSummary room)
    {
        room.Topic = Topic;
    }
}
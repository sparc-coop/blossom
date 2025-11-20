namespace Sparc.Blossom.Realtime;

public record RoomTopic(string Topic) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoom room)
    {
        room.Topic = Topic;
    }
}
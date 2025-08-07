namespace Sparc.Core.Chat;

public record MatrixMessage(string Body, string MsgType = "m.text");
public class MatrixMessageEvent(string roomId, string sender, MatrixMessage content, List<MatrixEvent>? previousEvents = null) 
    : MatrixEvent<MatrixMessage>("m.room.message", roomId, sender, content, previousEvents)
{
    public MatrixMessageEvent() : this(string.Empty, string.Empty, new MatrixMessage(""))
    {
    }

    public MatrixMessageEvent(string roomId, string sender, string body, string msgType = "m.text", List<MatrixEvent>? previousEvents = null)
        : this(roomId, sender, new(body, msgType), previousEvents)
    {
    }
}

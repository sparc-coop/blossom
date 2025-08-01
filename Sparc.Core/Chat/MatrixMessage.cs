using Sparc.Engine;

namespace Sparc.Core.Chat;

public class MatrixMessage
{
    public string MsgType { get; set; }
    public string Body { get; set; }
}

public class MatrixMessageEvent : MatrixEvent<MatrixMessage>
{
    public MatrixMessageEvent(string roomId, string sender, MatrixMessage content, List<MatrixEvent>? previousEvents = null)
        : base("m.room.message", roomId, sender, content, previousEvents)
    {
        if (string.IsNullOrWhiteSpace(content.MsgType))
        {
            content.MsgType = "m.text";
        }
    }
    public MatrixMessageEvent(string roomId, string sender, string body, string msgType = "m.text", List<MatrixEvent>? previousEvents = null)
        : this(roomId, sender, new MatrixMessage { MsgType = msgType, Body = body }, previousEvents)
    {
    }
}

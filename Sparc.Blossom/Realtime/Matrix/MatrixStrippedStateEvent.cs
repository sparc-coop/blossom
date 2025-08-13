namespace Sparc.Blossom.Realtime.Matrix;

public class MatrixStrippedStateEvent
{
    public string RoomId { get; set; } = "";
    public string Sender { get; set; } = "";
    public string? StateKey { get; set; }

    public MatrixStrippedStateEvent()
    { }
    public MatrixStrippedStateEvent(MatrixEvent ev)
    {
        RoomId = ev.RoomId;
        Sender = ev.Sender;
        StateKey = ev.StateKey;
    }
}

public class MatrixStrippedStateEvent<T> : MatrixStrippedStateEvent
{
    public T Content { get; set; }

    public MatrixStrippedStateEvent()
    {
        RoomId = string.Empty;
        Sender = string.Empty;
        StateKey = null;
        Content = default!;
    }

    public MatrixStrippedStateEvent(MatrixEvent<T> fullEvent)
    {
        RoomId = fullEvent.RoomId;
        Sender = fullEvent.Sender;
        StateKey = fullEvent.StateKey;
        Content = fullEvent.Content;
    }
}

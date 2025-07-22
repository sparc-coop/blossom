namespace Sparc.Core.Chat;

public class Event
{
    public string EventId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty; // UserId of the sender
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}



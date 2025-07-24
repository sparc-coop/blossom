using Sparc.Blossom;

namespace Sparc.Core.Chat;

public class Event() : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string RoomId { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

}



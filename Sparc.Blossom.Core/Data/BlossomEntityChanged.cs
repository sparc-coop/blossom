using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class BlossomEntityChanged(BlossomEntity entity) : IBlossomEvent
{
    public string? SubscriptionId { get; set; } = entity.GenericId.ToString();
}
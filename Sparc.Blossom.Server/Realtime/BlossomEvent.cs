using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public record BlossomEvent(BlossomEntity Entity) : IBlossomEvent
{
    public string? SubscriptionId => Entity.GenericId.ToString();
}

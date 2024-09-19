using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent(BlossomEntity Entity) : MediatR.INotification
{
    public string? SubscriptionId => $"{Entity.GetType().Name}-{Entity.GenericId}";
}

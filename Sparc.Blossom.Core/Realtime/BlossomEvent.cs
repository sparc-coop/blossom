using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent : MediatR.INotification
{
    private readonly BlossomEntity Entity;

    public BlossomEvent(BlossomEntity entity)
    {
        Entity = entity;
        ModifiedFields = new List<KeyValuePair<string, object?>>();
    }

    public BlossomEvent(BlossomEntity entity, List<KeyValuePair<string, object?>> modifiedFields)
    {
        Entity = entity;
        ModifiedFields = modifiedFields;
    }

    public string? SubscriptionId => $"{Entity.GetType().Name}-{Entity.GenericId}";

    public List<KeyValuePair<string, object?>> ModifiedFields { get; }
}

using Sparc.Blossom.Data;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent : MediatR.INotification
{
    public BlossomEntity Entity;

    public BlossomEvent(BlossomEntity entity)
    {
        Entity = entity;
    }

    public string? SubscriptionId => $"{Entity.GetType().Name}-{Entity.GenericId}";
}

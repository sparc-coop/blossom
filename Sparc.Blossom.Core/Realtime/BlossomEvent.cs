using Sparc.Blossom.Api;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent : MediatR.INotification
{
    private BlossomEvent(string name)
    {
        Name = name;
    }

    public BlossomEvent(BlossomEntity entity) : this(entity.GetType().Name)
    {
        EntityId = entity.GenericId.ToString();
        SubscriptionId = $"{entity.GetType().Name}-{EntityId}";
    }
    
    public BlossomEvent(string name, BlossomEntity entity) : this(entity)
    {
        Name = name;
    }

    public BlossomEvent(IBlossomEntityProxy proxy) : this(proxy.GetType().Name)
    {
        SubscriptionId = proxy.SubscriptionId;
    }

    public string Name { get; protected set; }
    public string EntityId { get; protected set; } = "";
    public long Id { get; protected set; } = DateTime.UtcNow.Ticks;
    public string? SubscriptionId { get; protected set; }
    public string? UserId { get; protected set; }
    public BlossomPatch? Changes { get; protected set; } = null;
    public long? PreviousId { get; protected set; }
    public List<long> FutureIds { get; protected set; } = [];

    public void SetUser(ClaimsPrincipal? user)
    {
        UserId = user?.Id();
    }
}

public class BlossomEvent<T>(T entity) : BlossomEvent(entity) where T : BlossomEntity
{
    public T Entity { get; private set; } = entity;

    public BlossomEvent(BlossomEvent<T> previous) : this(previous.Entity, previous)
    {
    }

    public BlossomEvent(string name, T entity) : this(entity)
    {
        Name = name;
    }

    public BlossomEvent(T entity, BlossomPatch changes) : this(entity)
    {
        Changes = changes;
    }

    public BlossomEvent(T entity, BlossomEvent<T> previous) : this(entity)
    {
        PreviousId = previous.Id;
        Changes = new BlossomPatch(previous.Entity, entity);

    }
}

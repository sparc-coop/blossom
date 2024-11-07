using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent : BlossomEntity<long>, MediatR.INotification
{
    private BlossomEvent(string name)
    {
        Id = DateTime.UtcNow.Ticks;
        Name = name;
    }

    public BlossomEvent(BlossomEntity entity) : this(entity.GetType().Name)
    {
        EntityType = entity.GetType().FullName;
        EntityId = entity.GenericId.ToString();
        SubscriptionId = $"{entity.GetType().Name}-{EntityId}";
    }
    
    public BlossomEvent(string name, BlossomEntity entity) : this(entity)
    {
        Name = name;
    }

    public string EntityType { get; protected set; } = "";
    public string EntityId { get; protected set; } = "";
    public string? SubscriptionId { get; protected set; }
    public string Name { get; protected set; }
    public string? UserId { get; protected set; }
    public List<BlossomPatch> Changes { get; protected set; } = [];
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

    public BlossomEvent(T entity, BlossomEvent<T> previous) : this(entity)
    {
        PreviousId = previous.Id;
        Changes = BlossomPatch.Create(previous.Entity, entity);

    }
}

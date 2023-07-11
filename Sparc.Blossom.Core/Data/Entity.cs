using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class Entity
{
    internal List<INotification>? _events;

    public void Broadcast(INotification notification)
    {
        _events ??= new List<INotification>();
        _events!.Add(notification);
    }

    public List<INotification> Publish()
    {
        if (_events == null || !_events.Any())
            return new();

        var domainEvents = _events.ToList();
        _events.Clear();

        return domainEvents;
    }

    public virtual object GenericId { get; } = null!;
}

public class Entity<T> : Entity, IEntity<T> where T : notnull
{
    public Entity()
    {
        Id = default!;
    }

    public Entity(T id) => Id = id;
    public override object GenericId => Id;

    public virtual T Id { get; set; }
}

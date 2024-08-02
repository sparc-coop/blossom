using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class BlossomEntity
{
    internal List<IBlossomEvent>? _events;

    public List<IBlossomEvent> Publish()
    {
        _events ??= [];

        var domainEvents = _events.ToList();
        domainEvents.Add(new BlossomEntityChanged(this));
        _events.Clear();

        return domainEvents;
    }

    protected void Broadcast<T>() where T : IBlossomEvent => Broadcast((T)Activator.CreateInstance(typeof(T), this));

    protected void Broadcast(IBlossomEvent notification)
    {
        _events ??= [];
        _events!.Add(notification);
    }

    //protected void On(INotification notification) => ((dynamic)this).On(notification);

    public virtual object GenericId { get; } = null!;
}

public class BlossomEntity<T> : BlossomEntity where T : notnull
{
    public BlossomEntity()
    {
        Id = default!;
    }

    public BlossomEntity(T id) => Id = id;
    public override object GenericId => Id;

    public virtual T Id { get; set; }
}

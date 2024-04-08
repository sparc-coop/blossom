using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class BlossomEntity
{
    internal List<INotification>? _events;

    public List<INotification> Publish()
    {
        if (_events == null)
            return [];

        var domainEvents = _events.ToList();
        _events.Clear();

        return domainEvents;
    }

    protected void Broadcast(INotification notification)
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

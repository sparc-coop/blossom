using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class Root
{
    internal List<INotification>? _events;

    public void Broadcast(INotification notification)
    {
        _events ??= new List<INotification>();
        _events!.Add(notification);
    }
}

public class Root<T> : Root, IRoot<T> where T : notnull
{
    public Root()
    {
        Id = default!;
    }

    public Root(T id) => Id = id;

    public virtual T Id { get; set; }
}

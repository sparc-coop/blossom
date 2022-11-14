using Sparc.Blossom;
using Sparc.Core;

namespace Sparc.Kernel;

public class Root
{
    internal List<Notification>? _events;

    public void Broadcast(Notification notification)
    {
        _events ??= new List<Notification>();
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

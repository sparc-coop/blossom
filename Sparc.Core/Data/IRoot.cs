using Sparc.Blossom;

namespace Sparc.Core;

public interface IRoot<T>
{
    T Id { get; set; }
    void Broadcast(Notification notification);
}
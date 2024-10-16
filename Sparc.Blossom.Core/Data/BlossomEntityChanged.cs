using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class BlossomEntityChanged(BlossomEntity entity) : BlossomEvent(entity);
public class BlossomEntityChanged<T>(T entity) : BlossomEntityChanged(entity) 
    where T : BlossomEntity
{
    public T Entity { get; private set; } = entity;
}
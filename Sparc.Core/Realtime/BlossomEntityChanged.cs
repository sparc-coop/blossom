using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public record BlossomEntityChanged(string EntityId) : BlossomEvent(EntityId)
{
    public BlossomEntityChanged(BlossomEntity entity) : this(entity.GenericId.ToString())
    {
        Type = entity.GetType().Name;
    }

    public BlossomPatch? Changes { get; set; } = null;
}

public record BlossomEntityChanged<T> : BlossomEntityChanged where T : BlossomEntity
{
    public T Entity { get; private set; }

    public BlossomEntityChanged(T entity) : base(entity)
    {
        Entity = entity;
    }
    
    public BlossomEntityChanged(T entity, BlossomPatch changes) : this(entity)
    {
        Changes = changes;
    }

    public BlossomEntityChanged(T entity, BlossomEntityChanged<T> previous) : this(entity)
    {
        Changes = new(previous.Entity, entity);
    }
}

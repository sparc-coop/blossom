using Ardalis.Specification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sparc.Blossom;

public class BlossomRealtimeEntity(BlossomEntity entity) : ObservableRecipient
{
    BlossomEntity GenericEntity { get; set; } = entity;

    internal void Patch(BlossomPatch? changes)
    {
        changes?.ApplyTo(GenericEntity);
    }
}

public class BlossomRealtimeEntity<T, TId>(T entity)
    : BlossomRealtimeEntity(entity)
    where T : BlossomEntity<TId>
    where TId : notnull
{
    protected T Entity { get; set; } = entity;
}

using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public class CreateBlossomRevision<T>(IRevisionRepository<T> revisions) : BlossomOn<BlossomEntityChanged<T>> where T : BlossomEntity
{
    public IRevisionRepository<T> Revisions { get; } = revisions;

    public override async Task ExecuteAsync(BlossomEntityChanged<T> item)
    {
        await Revisions.AddAsync(item.Entity.GenericId.ToString()!);
    }
}


using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom.Data;

public class BlossomRevision(BlossomEntity entity)
{
    public string Id { get; private set; } = entity.GenericId.ToString();
    public long Revision { get; protected set; } = DateTime.UtcNow.Ticks;
    public long? Previous { get; protected set; }
    public List<long> Future { get; set; } = [];
    public string UserId { get; set; } = ClaimsPrincipal.Current.Id();
}

public class BlossomRevision<T>(T entity) : BlossomRevision(entity) where T : BlossomEntity
{
    public T Entity { get; private set; } = entity;

    public static BlossomRevision<T> Create(T current, BlossomRevision<T>? previous)
    {
        var revision = new BlossomRevision<T>(current)
        {
            Previous = previous?.Revision
        };
        return revision;
    }

    public BlossomRevision<T> Undo(BlossomRevision<T> previous)
    {
        var revision = new BlossomRevision<T>(previous.Entity)
        {
            Previous = previous.Previous,
            Future = previous.Future
        };

        revision.Future.Insert(0, Revision);
        return revision;
    }

    public BlossomRevision<T> Redo(BlossomRevision<T> future)
    {
        if (Future.FirstOrDefault() != future.Revision)
            throw new Exception("Future entity revision does not match future reference in current");

        var revision = new BlossomRevision<T>(future.Entity)
        {
            Previous = Revision,
            Future = future.Future
        };

        revision.Future.Remove(0);
        return revision;
    }

    public BlossomRevision<T> UpdateTo(BlossomRevision<T> replaceWith)
    {
        return Future.Contains(replaceWith.Revision)
            ? Redo(replaceWith)
            : Undo(replaceWith);
    }
}

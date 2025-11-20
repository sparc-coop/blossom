using MediatR;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom;

public class BlossomRealtimeRepository<T>(IRepository<BlossomEntityChanged<T>> repository, IPublisher publisher, ClaimsPrincipal principal)
    : IRealtimeRepository<T>
    where T : BlossomEntity
{
    public IRepository<BlossomEntityChanged<T>> Repository { get; } = repository;
    public IPublisher Publisher { get; } = publisher;
    ClaimsPrincipal User => principal;
    string? UserId => User.Id();
    IQueryable<BlossomEntityChanged<T>> UserEvents => Repository.Query.Where(x => x.UserId == UserId);

    public async Task<BlossomEntityChanged<T>?> GetAsync(string id)
    {
        return (await GetAllAsync(id, 1)).FirstOrDefault();
    }

    public Task<BlossomEntityChanged<T>?> GetAsync(string id, long revision)
    {
        var item = Repository.Query.FirstOrDefault(x => x.EntityId == id && x.Id == revision);
        return Task.FromResult(item);
    }

    public async Task<BlossomEntityChanged<T>?> GetAsync(string id, DateTime? asOfDate = null)
    {
        var entityRevisions = await GetAllAsync(id);
        var ticks = (asOfDate?.ToUniversalTime() ?? DateTime.UtcNow).Ticks;
        return entityRevisions.FirstOrDefault(x => x.Id <= ticks);
    }

    public Task<IEnumerable<BlossomEntityChanged<T>>> GetAllAsync(string id, int? count = null)
    {
        IQueryable<BlossomEntityChanged<T>> events = Repository.Query
            .Where(x => x.EntityId == id)
            .OrderByDescending(x => x.Id);

        if (count.HasValue)
            events = events.Take(count.Value);

        return Task.FromResult<IEnumerable<BlossomEntityChanged<T>>>(events);
    }

    public async Task BroadcastAsync(string eventName, T entity)
    {
        var newEvent = new BlossomEntityChanged<T>(eventName, entity);
        await BroadcastAsync(newEvent);
    }

    public async Task BroadcastAsync(BlossomEntityChanged<T> newEvent)
    {
        newEvent.SetUser(User);
        await Repository.AddAsync(newEvent);
        await Publisher.Publish(newEvent);
    }

    public async Task<T?> UndoAsync(string id)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to undo");

        if (!current.PreviousId.HasValue)
            throw new Exception("No previous revision to undo");

        return await ReplaceAsync(current, current.PreviousId.Value);
    }

    public async Task<T?> RedoAsync(string id)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to redo");

        if (current.FutureIds.Count == 0)
            throw new Exception("No future revision to redo");

        return await ReplaceAsync(current, current.FutureIds[0]);
    }

    public async Task<T> ReplaceAsync(BlossomEntityChanged<T> current, long revision)
    {
        var replaceWith = await GetAsync(current.EntityId, revision)
            ?? throw new Exception("Revision to replace with not found");

        BlossomEntityChanged<T> changed = current.FutureIds.Contains(revision)
            ? new BlossomEntityRedone<T>(current, replaceWith)
            : new BlossomEntityUndone<T>(current, replaceWith);

        await BroadcastAsync(changed);
        return changed.Entity;
    }

    public async Task<T> ReplaceAsync(string id, long revision)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to replace");

        return await ReplaceAsync(current, revision);
    }
}

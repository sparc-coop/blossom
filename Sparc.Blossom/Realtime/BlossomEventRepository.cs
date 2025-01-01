using MediatR;
using System.Security.Claims;

namespace Sparc.Blossom;

public class BlossomEventRepository<T>(IRepository<BlossomEvent<T>> repository, IPublisher publisher, ClaimsPrincipal principal)
    : IEventRepository<T>
    where T : BlossomEntity
{
    public IRepository<BlossomEvent<T>> Events { get; } = repository;
    public IPublisher Publisher { get; } = publisher;
    ClaimsPrincipal User => principal;

    public async Task<BlossomEvent<T>?> GetAsync(string id)
    {
        return (await GetAllAsync(id, 1)).FirstOrDefault();
    }

    public Task<BlossomEvent<T>?> GetAsync(string id, long revision)
    {
        var item = Events.Query.FirstOrDefault(x => x.EntityId == id && x.Id == revision);
        return Task.FromResult(item);
    }

    public async Task<BlossomEvent<T>?> GetAsync(string id, DateTime? asOfDate = null)
    {
        var entityRevisions = await GetAllAsync(id);
        var ticks = (asOfDate?.ToUniversalTime() ?? DateTime.UtcNow).Ticks;
        return entityRevisions.FirstOrDefault(x => x.Id <= ticks);
    }

    public Task<IEnumerable<BlossomEvent<T>>> GetAllAsync(string id, int? count = null)
    {
        IQueryable<BlossomEvent<T>> events = Events.Query
            .Where(x => x.EntityId == id)
            .OrderByDescending(x => x.Id);

        if (count.HasValue)
            events = events.Take(count.Value);

        return Task.FromResult<IEnumerable<BlossomEvent<T>>>(events);
    }

    public async Task AddAsync(string eventName, T entity)
    {
        var newEvent = new BlossomEvent<T>(eventName, entity);
        await AddAsync(newEvent);
    }

    public async Task AddAsync(BlossomEvent<T> newEvent)
    {
        newEvent.SetUser(User);
        await Events.AddAsync(newEvent);
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

    public async Task<T> ReplaceAsync(BlossomEvent<T> current, long revision)
    {
        var replaceWith = await GetAsync(current.EntityId, revision)
            ?? throw new Exception("Revision to replace with not found");

        BlossomEvent<T> changed = current.FutureIds.Contains(revision)
            ? new BlossomEntityRedone<T>(current, replaceWith)
            : new BlossomEntityUndone<T>(current, replaceWith);

        await AddAsync(changed);
        return changed.Entity;
    }

    public async Task<T> ReplaceAsync(string id, long revision)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to replace");

        return await ReplaceAsync(current, revision);
    }
}

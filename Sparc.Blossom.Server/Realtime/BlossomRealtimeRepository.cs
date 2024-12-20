using MediatR;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtimeRepository<T>(IRepository<BlossomEvent<T>> repository, IPublisher publisher, IHttpContextAccessor http)
    : IRealtimeRepository<T>
    where T : BlossomEntity
{
    public IRepository<BlossomEvent<T>> Repository { get; } = repository;
    public IPublisher Publisher { get; } = publisher;
    ClaimsPrincipal? User => http?.HttpContext?.User;
    string? UserId => User?.Id();
    IQueryable<BlossomEvent<T>> UserEvents => Repository.Query.Where(x => x.UserId == UserId);

    public async Task<BlossomEvent<T>?> GetAsync(string id)
    {
        return (await GetAllAsync(id, 1)).FirstOrDefault();
    }

    public Task<BlossomEvent<T>?> GetAsync(string id, long revision)
    {
        var item = Repository.Query.FirstOrDefault(x => x.EntityId == id && x.Id == revision);
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
        IQueryable<BlossomEvent<T>> events = Repository.Query
            .Where(x => x.EntityId == id)
            .OrderByDescending(x => x.Id);

        if (count.HasValue)
            events = events.Take(count.Value);

        return Task.FromResult<IEnumerable<BlossomEvent<T>>>(events);
    }

    public async Task BroadcastAsync(string eventName, T entity)
    {
        var newEvent = new BlossomEvent<T>(eventName, entity);
        await BroadcastAsync(newEvent);
    }

    public async Task BroadcastAsync(BlossomEvent<T> newEvent)
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

    public async Task<T> ReplaceAsync(BlossomEvent<T> current, long revision)
    {
        var replaceWith = await GetAsync(current.EntityId, revision)
            ?? throw new Exception("Revision to replace with not found");

        BlossomEvent<T> changed = current.FutureIds.Contains(revision)
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

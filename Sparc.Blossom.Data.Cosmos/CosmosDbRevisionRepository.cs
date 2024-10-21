using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbRevisionRepository<T>(DbContext context) : IRevisionRepository<T> where T : BlossomEntity<string>
{
    public DbContext Context { get; } = context;

    public async Task<BlossomRevision<T>?> GetAsync(string id, long revision)
    {
        return await Context.Set<BlossomRevision<T>>()
            .AsNoTracking()
            .WithPartitionKey(id)
            .FirstOrDefaultAsync(x => x.Revision == revision);
    }

    public async Task<BlossomRevision<T>?> GetAsync(string id, DateTime? asOfDate = null)
    {
        var ticks = (asOfDate?.ToUniversalTime() ?? DateTime.UtcNow).Ticks;

        return await Context.Set<BlossomRevision<T>>()
            .WithPartitionKey(id)
            .AsNoTracking()
            .Where(x => x.Revision <= ticks)
            .OrderByDescending(x => x.Revision)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BlossomRevision<T>>> GetAllAsync(string id, int count)
    {
        return await Context.Set<BlossomRevision<T>>()
            .WithPartitionKey(id)
            .AsNoTracking()
            .OrderByDescending(x => x.Revision)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BlossomRevision<T>> UndoAsync(string id)
    {
        var currentEntity = await GetAsync(id)
            ?? throw new Exception("Current revision not found");

        if (currentEntity.Previous == null)
            throw new Exception("No previous revision to undo");

        return await ReplaceAsync(id, currentEntity.Previous.Value);
    }

    public async Task<BlossomRevision<T>> RedoAsync(string id)
    {
        var currentEntity = await GetAsync(id)
            ?? throw new Exception("Current revision not found");

        if (currentEntity.Future.Count == 0)
            throw new Exception("No future revision to redo");

        return await ReplaceAsync(id, currentEntity.Future[0]);
    }

    public async Task<BlossomRevision<T>> ReplaceAsync(string id, long replaceWithRevisionId)
    {
        var currentEntity = await GetAsync(id)
            ?? throw new Exception("Current revision not found");

        var replaceWith = await GetAsync(id, replaceWithRevisionId)
            ?? throw new Exception("Revision to replace with not found");

        var newRevision = currentEntity.UpdateTo(replaceWith);
        await AddAsync(newRevision);

        return newRevision;
    }

    public async Task AddAsync(string id)
    {
        var previous = await GetAsync(id);
        var current = await Context.Set<T>().FindAsync(id) 
            ?? throw new Exception("Entity not found");

        var revision = BlossomRevision<T>.Create(current, previous);
        Context.Set<BlossomRevision<T>>().Add(revision);
        await Context.SaveChangesAsync();
    }

    public async Task AddAsync(BlossomRevision<T> revision)
    {
        var existing = await Context.Set<T>().FindAsync(revision.Id);
        if (existing != null)
        {
            Context.Entry(existing).State = EntityState.Detached;
            Context.Add(revision.Entity);
            Context.Update(revision.Entity);
        }

        Context.Set<BlossomRevision<T>>().Add(revision);
        await Context.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbRevisionRepository<T>(DbContext context)
    : IRevisionRepository<T>
    where T : BlossomEntity<string>
{
    public DbContext Context { get; } = context;

    public async Task<BlossomRevision<T>?> FindAsync(string id)
    {
        var current = await GetAsync(id, 1);
        return current.FirstOrDefault();
    }

    public async Task<BlossomRevision<T>?> FindAsync(string id, long revision)
    {
        return await Context.Set<BlossomRevision<T>>()
            .AsNoTracking()
            .WithPartitionKey(id)
            .FirstOrDefaultAsync(x => x.Current == revision);
    }

    public async Task<IEnumerable<BlossomRevision<T>>> GetAsync(string id, int count)
    {
        return await Context.Set<BlossomRevision<T>>()
            .WithPartitionKey(id)
            .AsNoTracking()
            .OrderByDescending(x => x.Current)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BlossomRevision<T>> ReplaceAsync(string id, long replaceWithRevisionId)
    {
        var currentEntity = await FindAsync(id)
            ?? throw new Exception("Current revision not found");

        var replaceWith = await FindAsync(id, replaceWithRevisionId)
            ?? throw new Exception("Revision to replace with not found");

        var newRevision = currentEntity.Future.Contains(replaceWithRevisionId)
            ? BlossomRevision<T>.FromFuture(currentEntity, replaceWith)
            : BlossomRevision<T>.FromPrevious(currentEntity, replaceWith);

        await AddAsync(newRevision);

        return newRevision;
    }

    public async Task<BlossomRevision<T>> AddAsync(BlossomRevision<T> revision)
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
        
        return revision;
    }
}

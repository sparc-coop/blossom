using Ardalis.Specification;
using Microsoft.JSInterop;

namespace Sparc.Blossom.Data.Dexie;

public class DexieRepository<T>(DexieDatabase db) : IRepository<T>
    where T : BlossomEntity<T>
{
    public IQueryable<T> Query => throw new NotImplementedException();
    List<string> Indexes => db.Repositories[typeof(T).Name.ToLower()];

    public async Task AddAsync(T item)
    {
        var set = await db.Set<T>();
        await set.InvokeVoidAsync("add", item);
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        var set = await db.Set<T>();
        await set.InvokeVoidAsync("bulkAdd", items);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        var result = await FindAsync(spec);
        return result != null;
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        var query = await db.Query(spec);
        return await query.CountAsync();
    }

    public async Task DeleteAsync(T item)
    {
        var set = await db.Set<T>();
        await set.InvokeVoidAsync("delete", item.Id);
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        var set = await db.Set<T>();
        var ids = items.Select(x => x.Id).ToArray();
        await set.InvokeVoidAsync("bulkDelete", ids);
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await FindAsync(id);
        if (entity != null)
            await ExecuteAsync(entity, action);
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await UpdateAsync(entity);
    }

    public async Task<T?> FindAsync(object id)
    {
        var set = await db.Set<T>();
        var entity = await set.InvokeAsync<T?>("get", id);
        return entity;
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        var query = await db.Query(spec.Query.Skip(0).Take(1).Specification);
        var result = await query.ToListAsync();
        return result.FirstOrDefault();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        var result = await db.Query(spec);
        return await result.ToListAsync();
    }

    public async Task UpdateAsync(T item)
    {
        var set = await db.Set<T>();
        await set.InvokeVoidAsync("put", item);
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        var set = await db.Set<T>();
        await set.InvokeVoidAsync("bulkPut", items);
    }
}

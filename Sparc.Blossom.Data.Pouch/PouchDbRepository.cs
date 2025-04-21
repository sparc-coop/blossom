using Ardalis.Specification;
using Microsoft.JSInterop;
using Sparc.Blossom;

namespace Sparc.Blossom.Data;

public class PouchDbRepository<T>(IJSRuntime js) : IRepository<T>
{
    public IJSRuntime Js { get; } = js;
    readonly Lazy<Task<IJSObjectReference>> _pouch = js.Import("./_content/Sparc.Blossom.Data.Pouch/PouchRepository.mjs");
    static string DbName => typeof(T).Name.ToLower();

    public IQueryable<T> Query => GetAllAsync().Result.AsQueryable();

    public async Task AddAsync(T item) => await ExecuteAsync("add", item!);
    public async Task AddAsync(IEnumerable<T> items) => await ExecuteAsync("bulkAdd", items);
    
    public async Task<bool> AnyAsync(ISpecification<T> spec) => (await CountAsync(spec)) > 0;

    public async Task<int> CountAsync(ISpecification<T> spec) => (await GetAllAsync(spec)).Count();
    
    public async Task DeleteAsync(T item) => await ExecuteAsync("remove", item!);

    public async Task DeleteAsync(IEnumerable<T> items) => await ExecuteAsync("bulkRemove", items);

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

    public async Task<T?> FindAsync(object id) => await ExecuteAsync<T?>("find", id);

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        var pouchSpec = new PouchDbSpecification<T>(spec).Query;
        pouchSpec = pouchSpec with { Limit = 1 };
        var result = await ExecuteAsync<List<T>>("query", pouchSpec);
        return result.FirstOrDefault();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetAllAsync() => await ExecuteAsync<List<T>>("getAll", DbName);

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        var pouchSpec = new PouchDbSpecification<T>(spec).Query;
        return await ExecuteAsync<List<T>>("query", pouchSpec);
    }

    public async Task UpdateAsync(T item) => await ExecuteAsync("update", item!);

    public async Task UpdateAsync(IEnumerable<T> items) => await ExecuteAsync("bulkUpdate", items);

    async Task ExecuteAsync(string identifier, object item)
    {
        var pouch = await Pouch();
        await pouch.InvokeVoidAsync(identifier, DbName, item);
    }

    async Task<TResult> ExecuteAsync<TResult>(string identifier, object item)
    {
        var pouch = await Pouch();
        return await pouch.InvokeAsync<TResult>(identifier, DbName, item);
    }

    async Task<IJSObjectReference> Pouch()
    {
        var pouch = await _pouch.Value;
        return pouch;
    }

    public async Task<int> CountAsync()
    {
        return await ExecuteAsync<int>("count", DbName);
    }

    public async Task<List<T>> SyncAsync()
    {
        return await ExecuteAsync<List<T>>("syncAll", DbName);
    }

}

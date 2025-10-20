using Ardalis.Specification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Sparc.Blossom;

public class DexieRepository<T>(IServiceProvider services) : IRepository<T>
{
    public IJSRuntime Js => services.GetRequiredService<IJSRuntime>();
    Lazy<Task<IJSObjectReference>> _dexie => Js.Import("./Blossom/js/dexie/DexieRepository.js");
    static string DbName => typeof(T).Name.ToLower();

    public IQueryable<T> Query => throw new NotImplementedException();

    public async Task AddAsync(T item) => await ExecuteAsync("add", item!);
    public async Task AddAsync(IEnumerable<T> items) => await ExecuteAsync("bulkAdd", items);
    
    public Task<bool> AnyAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }
    
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

    public Task<T?> FindAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetAllAsync() => await ExecuteAsync<List<T>>("getAll", null);

    public async Task<List<T>> GetAllAsync(long? asOfRevision = null) => await ExecuteAsync<List<T>>("getAll", asOfRevision);

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(T item) => await ExecuteAsync("update", item!);

    public async Task UpdateAsync(IEnumerable<T> items) => await ExecuteAsync("bulkUpdate", items);

    async Task ExecuteAsync(string identifier, object item)
    {
        var dexie = await Dexie();
        await dexie.InvokeVoidAsync(identifier, DbName, item);
    }

    async Task<TResult> ExecuteAsync<TResult>(string identifier, object? item)
    {
        var dexie = await Dexie();
        return await dexie.InvokeAsync<TResult>(identifier, DbName, item);
    }

    async Task<IJSObjectReference> Dexie()
    {
        var dexie = await _dexie.Value;
        return dexie;
    }
}

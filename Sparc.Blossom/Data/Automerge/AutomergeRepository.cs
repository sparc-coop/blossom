using Ardalis.Specification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Sparc.Blossom;

public class AutomergeRepository<T>(IServiceProvider services) : IRepository<T>
{
    public IJSRuntime Js => services.GetRequiredService<IJSRuntime>();
    Lazy<Task<IJSObjectReference>> _automerge => Js.Import("./Blossom/js/automerge/AutomergeRepository.js");
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

    public async Task<T?> FindAsync(object id)
    {
        var automerge = await Automerge();
        return await automerge.InvokeAsync<T?>("find", CancellationToken.None, DbName, id);
    }

    public Task<T?> FindAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetAllAsync() {
        var automerge = await Automerge();
        var result = await automerge.InvokeAsync<List<T>>("getAll", CancellationToken.None, DbName, null);
        return result ?? [];
    }

    public async Task<List<T>> GetAllAsync(long? asOfRevision = null) {
        var automerge = await Automerge();
        var result = await automerge.InvokeAsync<List<T>>("getAll", CancellationToken.None, DbName, asOfRevision);
        return result ?? [];
    }

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(T item) => await ExecuteAsync("update", item!);

    public async Task UpdateAsync(IEnumerable<T> items) => await ExecuteAsync("bulkUpdate", items);

    async Task ExecuteAsync(string identifier, object item)
    {
        var automerge = await Automerge();
        await automerge.InvokeVoidAsync(identifier, CancellationToken.None, DbName, item);
    }

    async Task<IJSObjectReference> Automerge() => await _automerge.Value;
}

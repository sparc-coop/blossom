using Ardalis.Specification;
using Microsoft.JSInterop;

namespace Sparc.Blossom.Kori;

public class KoriRepository<T>(IJSRuntime js) : IRepository<T>
{
    public IJSRuntime Js { get; } = js;
    readonly Lazy<Task<IJSObjectReference>> Pouch = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Blossom/js/pouchdb-9.0.0.min.js").AsTask());

    string DbName => typeof(T).Name;

    public IQueryable<T> Query => throw new NotImplementedException();

    public async Task AddAsync(T item) => await ExecuteAsync("put", item!);
    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await AddAsync(item);
    }

    public Task<bool> AnyAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(T item) => await ExecuteAsync("remove", item!);

    public Task DeleteAsync(IEnumerable<T> items)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(object id, Action<T> action)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(T entity, Action<T> action)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> FindAsync(object id) => await ExecuteAsync<T?>("get", id);

    public Task<T?> FindAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(T item) => await ExecuteAsync("update", item!);

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await UpdateAsync(item);
    }

    async Task ExecuteAsync(string identifier, params object[] args)
    {
        var pouch = await Pouch.Value;
        await pouch.InvokeVoidAsync(identifier, DbName, args);
    }

    async Task<TResult> ExecuteAsync<TResult>(string identifier, params object[] args)
    {
        var pouch = await Pouch.Value;
        return await pouch.InvokeAsync<TResult>(identifier, DbName, args);
    }
}

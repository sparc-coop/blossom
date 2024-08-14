using Ardalis.Specification;
using System.Text.Json;

namespace Sparc.Blossom.Data;

public class BlossomSet<T> : IRepository<T> where T : class
{
    internal static List<T> _items = [];

    public IQueryable<T> Query => _items.AsQueryable();

    public Task AddAsync(T item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await AddAsync(item);
    }

    public Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).Any());
    }

    public Task<int> CountAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).Count());
    }

    public Task DeleteAsync(T item)
    {
        _items.Remove(item);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await DeleteAsync(item);
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var item = await FindAsync(id);
        if (item != null)
            await ExecuteAsync(item, action);
    }

    public Task ExecuteAsync(T entity, Action<T> action)
    {
        if (entity != null)
            action(entity);

        return Task.CompletedTask;
    }

    public Task<T?> FindAsync(object id)
    {
        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<string>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<string>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<int>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<int>>();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        throw new Exception("The item for this repository is not a Root.");
    }

    public Task<T?> FindAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).FirstOrDefault());
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return Task.FromResult(spec.Evaluate(_items).ToList());
    }

    public async Task UpdateAsync(T item)
    {
        object? id = (item as BlossomEntity<string>)?.Id;
        id ??= (item as BlossomEntity<int>)?.Id;

        if (id == null)
            throw new Exception("The item passed to UpdateAsync has no Id set.");

        var existingItem = await FindAsync(id);
        if (existingItem != null)
            await DeleteAsync(existingItem);

        await AddAsync(item);
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await UpdateAsync(item);
    }

    internal void Add(IEnumerable<T> items)
    {
        foreach (var item in items)
            _items.Add(item);
    }

    internal static BlossomSet<T> FromEnumerable(IEnumerable<T> items)
    {
        var set = new BlossomSet<T>();
        _items = items.ToList();
        return set;
    }

    internal static BlossomSet<T> FromUrl<TResponse>(string url, Func<TResponse, IEnumerable<T>> transformer)
    {
        using var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
        var response = client.Send(webRequest);
        if (!response.IsSuccessStatusCode)
            return new();

        using var reader = new StreamReader(response.Content.ReadAsStream());
        var json = reader.ReadToEnd();

        var items = JsonSerializer.Deserialize<TResponse>(json);
        if (items != null)
            return FromEnumerable(transformer(items));

        return new();
    }
}

using Ardalis.Specification;
using System.Text.Json;

namespace Sparc.Blossom;

public class BlossomRepository<T>(DexieRepository<T> dexie) : IRepository<T> 
    where T : class
{
    internal static List<T> _items = [];

    public IQueryable<T> Query => _items.AsQueryable();

    public async Task AddAsync(T item)
    {
        BlossomRepository<T>.UpdateTimestamp(item);
        _items.Add(item);
        await dexie.AddAsync(item);
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await AddAsync(item);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        await SyncAsync();
        return spec.Evaluate(_items).Any();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        await SyncAsync();
        return spec.Evaluate(_items).Count();
    }

    public async Task DeleteAsync(T item)
    {
        BlossomRepository<T>.UpdateTimestamp(item);
        try
        {
            _items.Remove(item);
            await dexie.DeleteAsync(item);
        }
        catch
        { }
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

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        if (entity != null)
        {
            action(entity);
            await UpdateAsync(entity);
        }
    }

    public async Task<T?> FindAsync(object id)
    {
        await SyncAsync();
        return FindInternalAsync(id);
    }

    private static T? FindInternalAsync(object id)
    {
        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<string>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<string>>().ToList();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return item;
        }

        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<int>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<int>>().ToList();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return item;
        }

        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<DateTime>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<DateTime>>().ToList();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return item;
        }

        throw new Exception("The item for this repository is not a Root.");
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        await SyncAsync();
        return spec.Evaluate(_items).FirstOrDefault();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetAllAsync()
    {
        await SyncAsync();
        return _items.ToList();
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        await SyncAsync();
        return spec.Evaluate(_items).ToList();
    }

    public async Task UpdateAsync(T item)
    {
        object? id = ((item as BlossomEntity)?.GenericId) 
            ?? throw new Exception("The item passed to UpdateAsync has no Id set.");
        
        var existingItem = await FindAsync(id);
        if (existingItem != null)
            _items.Remove(existingItem);

        BlossomRepository<T>.UpdateTimestamp(item);
        _items.Add(item);
        await dexie.UpdateAsync(item);
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            await UpdateAsync(item);
    }

    public async Task PatchAsync<U>(object id, U patch)
    {
        var item = await FindAsync(id) ?? throw new Exception("Item not found");

        // Update matching properties on the item with the patch
        var json = JsonSerializer.Serialize(patch);
        var patchItem = JsonSerializer.Deserialize<U>(json);
        var properties = typeof(U).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(patchItem);
            if (value != null)
                property.SetValue(item, value);
        }

        BlossomRepository<T>.UpdateTimestamp(item);
        await dexie.UpdateAsync(item);
    }

    internal async Task Add(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            BlossomRepository<T>.UpdateTimestamp(item);
            _items.Add(item);
        }

        await dexie.AddAsync(items);
    }

    public async Task AddFromUrlAsync<TResponse>(string url, Func<TResponse, IEnumerable<T>> transformer)
    {
        using var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await client.SendAsync(webRequest);
        if (!response.IsSuccessStatusCode)
            return;

        using var reader = new StreamReader(response.Content.ReadAsStream());
        var json = reader.ReadToEnd();

        var items = JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions {  PropertyNameCaseInsensitive = true });
        if (items != null)
        {
            _items.Clear();
            _items.AddRange(transformer(items));
        }
    }

    public async Task<int> CountAsync()
    {
        await SyncAsync();
        return _items.Count;
    }

    public async Task SyncAsync()
    {
        Console.WriteLine($"Syncing {typeof(T).Name}...");
        try
        {
            if (!typeof(T).IsAssignableTo(typeof(BlossomEntity)))
            {
                _items.Clear();
                _items.AddRange(await dexie.GetAllAsync());
            }
            else
            {
                var asOfRevision = _items.OfType<BlossomEntity>().Max(x => x.Revision);
                var items = await dexie.GetAllAsync(asOfRevision);
                Console.WriteLine($"  Found {items.Count} new items for {typeof(T).Name} since revision {asOfRevision}.");

                foreach (var item in items)
                {
                    var id = (item as BlossomEntity)?.GenericId;
                    if (id == null)
                        continue;
                    var existingItem = FindInternalAsync(id);
                    if (existingItem != null)
                        _items.Remove(existingItem);

                    _items.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing {typeof(T).Name}: {ex.Message}");
        }
    }

    private static void UpdateTimestamp(T item)
    {
        if (item is BlossomEntity entity)
            entity.Revision = DateTime.UtcNow.Ticks;
    }
}
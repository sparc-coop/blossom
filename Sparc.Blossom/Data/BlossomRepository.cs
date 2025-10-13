﻿using Ardalis.Specification;
using System.Text.Json;

namespace Sparc.Blossom;

public class BlossomRepository<T>(DexieRepository<T> dexie) : IRepository<T> where T : class
{
    public void Load(IEnumerable<T> items)
    {
        if (items.Any())
            _items = items.ToList();
    }
    
    internal static List<T> _items = [];

    public IQueryable<T> Query => _items.AsQueryable();

    public async Task AddAsync(T item)
    {
        _items.Add(item);
        await dexie.AddAsync(item);
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
        try
        {
            _items.Remove(item);
        }
        catch
        { }
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
            var itemsWithStringIds = _items.Cast<BlossomEntity<string>>().ToList();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<int>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<int>>().ToList();
            var item = itemsWithStringIds.FirstOrDefault(x => x.Id.Equals(id) == true) as T;
            return Task.FromResult(item);
        }

        if (typeof(T).IsAssignableTo(typeof(BlossomEntity<DateTime>)))
        {
            var itemsWithStringIds = _items.Cast<BlossomEntity<DateTime>>().ToList();
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

    public Task<List<T>> GetAllAsync()
    {
        return Task.FromResult(_items);
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
    }

    internal void Add(IEnumerable<T> items)
    {
        foreach (var item in items)
            _items.Add(item);
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
            Load(transformer(items));
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(_items.Count());
    }

    public Task<List<T>> SyncAsync()
    {
        throw new NotImplementedException();
    }
}

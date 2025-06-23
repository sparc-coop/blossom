using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbSimpleRepository<T>(CosmosDbSimpleClient<T> simpleClient, IMediator mediator) 
    : RepositoryBase<T>(simpleClient.Context), IRepository<T>
    where T : BlossomEntity<string>
{
    public IQueryable<T> Query { get; } = simpleClient.Container.GetItemLinqQueryable<T>();
    public CosmosDbSimpleClient<T> Client { get; } = simpleClient;
    public IMediator Mediator { get; } = mediator;

    public async Task<T?> FindAsync(object id)
    {
        var strId = id.ToString();
        var result = await Query.Where(x => x.Id == strId).ToCosmosAsync();
        return result.FirstOrDefault();
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        var result = await ApplySpecification(spec).ToCosmosAsync();
        return result.FirstOrDefault();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await CountAsync(spec, default);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return await AnyAsync(spec, default);
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task AddAsync(T item)
    {
        await AddAsync([item]);
    }

    public virtual async Task AddAsync(IEnumerable<T> items)
    {
        if (!items.Any())
            return;
        
        await Parallel.ForEachAsync(items, async (item, token) =>
        {
            try
            {
                await Client.Container.CreateItemAsync(item, cancellationToken: token);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Handle 409 Conflict (item already exists)
                Console.WriteLine($"Conflict: Item with id {item.Id} already exists.");
            }
            await Publish(item);
        });
    }

    private async Task Publish(T item)
    {
        var events = item.Publish();
        try
        {
            foreach (var ev in events)
                await Mediator.Publish(ev);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task UpdateAsync(T item)
    {
        await UpdateAsync([item]);
    }

    public virtual async Task UpdateAsync(IEnumerable<T> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) =>
        {
            try
            {
                await Client.Container.UpsertItemAsync(item, cancellationToken: token);
                await Publish(item);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await FindAsync(id);
        if (entity == null)
            throw new Exception($"Item with id {id} not found");

        await ExecuteAsync(entity, action);
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await UpdateAsync(entity);
    }

    public async Task DeleteAsync(T item)
    {
        await DeleteAsync([item]);
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            var partitionKey = GetPartitionKey(item);
            await Client.Container.DeleteItemAsync<T>(item.Id, partitionKey);
            await Publish(item);
        }
    }

    public PartitionKey GetPartitionKey(T item)
    {
        var partitionKeyProperty = Client.EntityType?.GetPartitionKeyProperties();
        if (partitionKeyProperty == null || partitionKeyProperty.Count == 0)
            return PartitionKey.None;

        var partitionKey = new PartitionKeyBuilder();
        foreach (var property in partitionKeyProperty)
        {
            var value = item.GetType().GetProperty(property.Name)?.GetValue(item)?.ToString();
            partitionKey.Add(value);
        }

        return partitionKey.Build();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        var results = FromSqlAsync<T>(sql, null, parameters).Result;
        return results.AsQueryable();
    }

    public async Task<List<U>> FromSqlAsync<U>(string sql, string? partitionKey, params object[] parameters)
    {
        var requestOptions = partitionKey == null
            ? null
            : new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };

        sql = sql.Replace("{", "@").Replace("}", "");

        var query = new QueryDefinition(sql);
        if (parameters != null)
        {
            var i = 0;
            foreach (var parameter in parameters)
            {
                var key = $"@{i++}";
                query = query.WithParameter(key, parameter);
            }
        }

        var results = Client.Container.GetItemQueryIterator<U>(query,
            requestOptions: requestOptions);

        var list = new List<U>();

        while (results.HasMoreResults)
            list.AddRange(await results.ReadNextAsync());

        return list;
    }

    public IQueryable<T> PartitionQuery(string partitionKey)
    {
        return simpleClient.Container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) });
    }
}

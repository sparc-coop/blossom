using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sparc.Blossom.Data;

public class CosmosDbSimpleRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : BlossomEntity<string>
{
    public IQueryable<T> Query { get; }
    public CosmosClient Client { get; }
    public Container Container { get; }
    public IMediator Mediator { get; }
    public IEntityType? EntityType { get; }

    public CosmosDbSimpleRepository(DbContext context, IMediator mediator) : base(context)
    {
        var databaseName = context.Database.GetCosmosDatabaseId();
        EntityType = context.Model.FindEntityType(typeof(T));
        Client = context.Database.GetCosmosClient();
        Client.ClientOptions.Serializer = new CosmosDbSimpleSerializer();

        var containerName = (EntityType?.GetContainer())
            ?? throw new Exception($"Container name not found for entity type {typeof(T)}");
        Container = Client.GetContainer(databaseName, containerName);

        Query = Container.GetItemLinqQueryable<T>();
        Mediator = mediator;
    }

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
        foreach (var item in items)
        {
            await Container.CreateItemAsync(item);
            await Publish(item);
        }

        await SaveChangesAsync();
    }

    private async Task Publish(BlossomEntity item)
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
        foreach (var item in items)
        {
            try
            {
                await Container.UpsertItemAsync(item);
                await Publish(item);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
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
            await Container.DeleteItemAsync<T>(item.Id, partitionKey);
            await Publish(item);
        }
    }

    private PartitionKey GetPartitionKey(T item)
    {
        var partitionKeyProperty = EntityType?.GetPartitionKeyProperties();
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
            : new QueryRequestOptions { PartitionKey = NewSparcHierarchicalPartitionKey(partitionKey) };

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

        var results = Container.GetItemQueryIterator<U>(query,
            requestOptions: requestOptions);

        var list = new List<U>();

        while (results.HasMoreResults)
            list.AddRange(await results.ReadNextAsync());

        return list;
    }

    public async Task UpsertAsync(dynamic item, string? partitionKey = null)
    {
        var pk = partitionKey != null ? NewSparcHierarchicalPartitionKey(partitionKey) : GetPartitionKey(item);

        await Container.UpsertItemAsync(item, pk);

        await Publish(item);
    }

    public async Task UpsertAsync(IEnumerable<T> items, string? partitionKey = null)
    {
        foreach (var item in items)
        {
            await UpsertAsync(item, partitionKey);
        }
    }

    public IQueryable<T> PartitionQuery(string partitionKey)
    {
        return Query.WithPartitionKey(partitionKey);
    }

    private static PartitionKey NewSparcHierarchicalPartitionKey(string partitionKey)
    {
        return new PartitionKeyBuilder().Add("sparc").Add("sparc-admin").Add(partitionKey).Build();
    }
}

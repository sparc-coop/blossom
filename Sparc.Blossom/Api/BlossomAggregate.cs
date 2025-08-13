using System.Security.Claims;
using System.Linq.Dynamic.Core;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace Sparc.Blossom;

public class BlossomAggregate<T>(BlossomAggregateOptions<T> options)
    : IRunner<T> where T : BlossomEntity
{
    public IRepository<T> Repository => options.Repository;
    public ClaimsPrincipal User => options.User;
    //public IRealtimeRepository<T> Events => options.Events;

    public virtual async Task<T?> Get(object id) => await Repository.FindAsync(id);

    public virtual async Task<T> Create(params object?[] parameters)
    {
        var entity = (T)Activator.CreateInstance(typeof(T), parameters)!;
        // await Events.BroadcastAsync(new BlossomEntityAdded<T>(entity));
        await Repository.AddAsync(entity);
        return entity;
    }

    protected BlossomQuery<T> Query() => new(Repository);

    protected virtual BlossomQuery<T> Query(BlossomQueryOptions options) => Query().WithOptions(options);

    public async Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options)
    {
        var query = Query(options);
        var results = await query.Execute();
        var count = await Repository.CountAsync(query);

        return new BlossomQueryResult<T>(results, count);
    }

    public async Task<IEnumerable<T>> ExecuteQuery(string? name = null, params object?[] parameters)
    {
        if (name == null)
            return Repository.Query;

        // Find the matching method and parameters in this type
        var query = GetType().GetMethod(name)
            ?? throw new Exception($"Method {name} not found.");


        var spec = query.Invoke(this, parameters) as BlossomQuery<T>
            ?? throw new Exception($"Specification {name} not found.");

        return await spec.Execute();
    }

    public async Task<TResponse?> ExecuteQuery<TResponse>(string name, params object?[] parameters)
    {
        var func = GetType().GetMethod(name)
            ?? throw new Exception($"Method {name} returning {typeof(TResponse).Name} not found.");

        var task = (Task<TResponse?>?)func.Invoke(this, parameters) 
            ?? throw new Exception($"Method {name} did not return a Task<{typeof(TResponse).Name}>.");
        
        return await task;
    }

    public Task<BlossomAggregateMetadata> Metadata()
    {
        var metadata = new BlossomAggregateMetadata(DtoType!);

        foreach (var property in metadata.Properties.Where(x => x.CanEdit && x.IsPrimitive))
        {
            var query = Repository.Query.GroupBy(property.Name).Select("new { Key, Count() as Count }");
            property.SetAvailableValues(query.ToDynamicList().ToDictionary(x => (object)x.Key ?? "<null>", x => (int)x.Count));
        }

        foreach (var relationship in metadata.Properties.Where(x => x.CanEdit && x.IsEnumerable))
        {
            var query = Repository.Query.SelectMany(relationship.Name).GroupBy("Id").Select("new { Key, Count() as Count, First() as First }").ToDynamicList();
            relationship.SetAvailableValues(query.Sum(x => (int)x.Count), query.ToDictionary(x => $"{x.Key}", x => ToDto(x.First)));
        }

        return Task.FromResult(metadata);
    }

    public async Task Patch(object id, BlossomPatch changes)
    {
        var entity = await Get(id);
        if (entity == null)
            return;

        changes.ApplyTo(entity);
        //await Events.BroadcastAsync(new BlossomEntityPatched<T>(entity, changes));
        await Repository.UpdateAsync(entity);
    }

    public async Task<T> Execute(object id, Action<T> action)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new Exception($"Entity {id} not found.");
        // await Events.BroadcastAsync(new BlossomEntityUpdated<T>(entity));
        await Repository.ExecuteAsync(entity, action);
        return entity;
    }

    public async Task<T> Execute(object id, string name, params object?[] parameters)
    {
        var action = new Action<T>(x => typeof(T).GetMethod(name)?.Invoke(x, parameters));
        return await Execute(id, action);
    }

    public async Task<T> Update(T entity)
    {
        var existing = await Get(entity.GenericId) 
            ?? throw new Exception($"Entity with id {entity.GenericId} not found.");
        
        var changes = new BlossomPatch(existing, entity);
        await Patch(entity.GenericId, changes);

        var updated = await Get(entity.GenericId)
            ?? throw new Exception($"Failed to retrieve updated entity with id {entity.GenericId}.");

        return updated;
    }

    public async Task Delete(object id)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new Exception($"Entity {id} not found.");

        // await Events.BroadcastAsync(new BlossomEntityDeleted<T>(entity));
        await Repository.DeleteAsync(entity);
    }

    public Task On(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<T?> Undo(object id, long? revision)
    {
        throw new NotImplementedException();
        
        //var strId = id.ToString()!;

        //return !revision.HasValue
        //    ? await Events.UndoAsync(strId)
        //    : await Events.ReplaceAsync(strId, revision.Value);
    }

    public Task<T?> Redo(object id, long? revision)
    {
        throw new NotImplementedException();
        //var strId = id.ToString()!;

        //return !revision.HasValue
        //    ? await Events.RedoAsync(strId)
        //    : await Events.ReplaceAsync(strId, revision.Value);
    }

    public Type? DtoType => AppDomain.CurrentDomain.FindType($"Sparc.Blossom.Api.{typeof(T).Name}");

    public object? ToDto<TItem>(TItem entity)
    {
        var dtoTypeName = $"Sparc.Blossom.Api.{typeof(TItem).Name}";
        var dtoType = AppDomain.CurrentDomain.FindType(dtoTypeName);
        return dtoType == null ? null : entity!.Adapt(typeof(TItem), dtoType);
    }
}

public class BlossomAggregateOptions<T>(IRepository<T> repository, ClaimsPrincipal principal)
    where T : BlossomEntity
{
    public IRepository<T> Repository { get; } = repository;
    public ClaimsPrincipal User => principal;
}

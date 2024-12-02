﻿using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Security.Claims;
using System.Linq.Dynamic.Core;

namespace Sparc.Blossom.Api;

public class BlossomAggregate<T>(BlossomAggregateOptions<T> options)
    : IRunner<T>, IBlossomEndpointMapper, IBlossomAggregate where T : BlossomEntity
{
    public string Name => typeof(T).Name;

    public IRepository<T> Repository => options.Repository;
    public IRealtimeRepository<T> Events => options.Events;
    protected ClaimsPrincipal? User => options.Http.HttpContext?.User;

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

    public Task<BlossomAggregateMetadata> Metadata()
    {
        if (BlossomAggregateOptions<T>.Metadata != null)
            return Task.FromResult(BlossomAggregateOptions<T>.Metadata);

        var metadata = new BlossomAggregateMetadata(typeof(T));
        foreach (var property in metadata.EditProperties.Where(x => x.IsPrimitive))
        {
            var query = Repository.Query.GroupBy(property.Name).Select("new { Key, Count() as Count }");
            property.SetAvailableValues(query.ToDynamicList().ToDictionary(x => (object)x.Key ?? "", x => (int)x.Count));
        }

        foreach (var relationship in metadata.EditProperties.Where(x => x.IsEnumerable))
        {
            var query = Repository.Query.SelectMany(relationship.Name).GroupBy("Id").Select("new { Key, Count() as Count, First() as First }").ToDynamicList();
            relationship.SetAvailableValues(query.Sum(x => (int)x.Count), query.ToDictionary(x => $"{x.Key}", x => (string)x.First.ToString()));
        }

        BlossomAggregateOptions<T>.Metadata = metadata;

        return Task.FromResult(metadata);
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

    public async Task Patch<U>(object id, U item)
    {
        await Repository.PatchAsync(id, item);
    }

    public async Task Execute(object id, string name, params object?[] parameters)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new Exception($"Entity {id} not found.");

        var action = new Action<T>(x => typeof(T).GetMethod(name)?.Invoke(x, parameters));
        // await Events.BroadcastAsync(name, entity);
        await Repository.ExecuteAsync(id, action);
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

    public async Task<T?> Undo(object id, long? revision)
    {
        var strId = id.ToString()!;

        return !revision.HasValue
            ? await Events.UndoAsync(strId)
            : await Events.ReplaceAsync(strId, revision.Value);
    }

    public async Task<T?> Redo(object id, long? revision)
    {
        var strId = id.ToString()!;

        return !revision.HasValue
            ? await Events.RedoAsync(strId)
            : await Events.ReplaceAsync(strId, revision.Value);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/{Name.ToLower()}";
        var group = endpoints.MapGroup(baseUrl);
        group.MapGet("{id}", async (IRunner<T> runner, string id) => await runner.Get(id));
        group.MapGet("_metadata", async (IRunner<T> runner, string id) => await runner.Metadata());
        group.MapPost("", async (IRunner<T> runner, object[] parameters) => await runner.Create(parameters));
        group.MapPost("_undo", async (IRunner<T> runner, string id, long? revision) => await runner.Undo(id, revision));
        group.MapPost("_redo", async (IRunner<T> runner, string id, long? revision) => await runner.Redo(id, revision));
        group.MapPost("{name}", async (IRunner<T> runner, string name, object[] parameters) => await runner.ExecuteQuery(name, parameters));
        group.MapPatch("{id}", async (IRunner<T> runner, string id, object patch) => await runner.Patch(id, patch));
        group.MapGet("{name}_flex", async (IRunner<T> runner, string name, BlossomQueryOptions options, object[] parameters) => await runner.ExecuteQuery(name, options, parameters));
        group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.Execute(id, name, parameters));
        group.MapDelete("{id}", async (IRunner<T> runner, string id) => await runner.Delete(id));
    }
}

public class BlossomAggregateOptions<T>(IRepository<T> repository, IRealtimeRepository<T> events, IHttpContextAccessor http)
    where T : BlossomEntity
{
    public IRepository<T> Repository { get; } = repository;
    public IRealtimeRepository<T> Events { get; } = events;
    public IHttpContextAccessor Http { get; } = http;
    public static BlossomAggregateMetadata? Metadata { get; set; }
}
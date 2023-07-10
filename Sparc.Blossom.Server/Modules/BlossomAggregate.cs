﻿using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom;

public interface IBlossomAggregate
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public abstract class BlossomAggregate<T> : IBlossomAggregate
{
    public BlossomAggregate()
    {
    }

    public virtual string Name => typeof(T).Name + "s";

    protected RouteGroupBuilder AggregateEndpoints = null!;
    protected RouteGroupBuilder EntityEndpoints = null!;
    protected string BaseUrl => $"/{Name.ToLower()}";

    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapBaseEndpoints(endpoints);
    }

    protected void MapBaseEndpoints(IEndpointRouteBuilder endpoints)
    {
        AggregateEndpoints = endpoints.MapGroup(BaseUrl);

        AggregateEndpoints.MapGet("", GetAllAsync ?? DefaultGetAllAsync).WithName($"GetAll{Name}").WithOpenApi();
        AggregateEndpoints.MapPost("", CreateAsync ?? DefaultCreateAsync).WithName($"Create{typeof(T).Name}").WithOpenApi();

        EntityEndpoints = AggregateEndpoints.MapGroup("{id}");

        EntityEndpoints.MapGet("", DefaultGetAsync).WithName($"Get{typeof(T).Name}").WithOpenApi();
        EntityEndpoints.MapPut("", UpdateAsync ?? DefaultUpdateAsync).WithName($"Update{typeof(T).Name}").WithOpenApi();
        EntityEndpoints.MapDelete("", DeleteAsync ?? DefaultDeleteAsync).WithName($"Delete{typeof(T).Name}").WithOpenApi();

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        foreach (var command in typeof(T).GetMethods(bindingFlags).Where(m => !m.IsSpecialName))
        {
            var factory = RequestDelegateFactory.Create(command, context => (T)context.Items["entity"]!, null);
            EntityEndpoints.MapPut(command.Name, factory.RequestDelegate).WithName(command.Name).WithOpenApi();
        }
    }

    protected async Task<Ok<T>> DefaultGetAsync(string id, IRepository<T> repository)
    {
        return TypedResults.Ok(await repository.FindAsync(id));
    }

    protected async Task<Ok<List<T>>> DefaultGetAllAsync(IRepository<T> repository)
    {
        var results = await repository.GetAllAsync(new BlossomGetAllSpecification<T>(null, 100));
        return TypedResults.Ok(results);
    }

    protected async Task<Created<T>> DefaultCreateAsync([FromBody] T entity, IRepository<T> repository)
    {
        await repository.AddAsync(entity);
        return TypedResults.Created($"{BaseUrl}/{entity.Id}", entity);
    }

    protected async Task<Results<NotFound, Ok<T>>> DefaultUpdateAsync([FromBody] T entity, IRepository<T> repository)
    {
        await repository.UpdateAsync(entity);
        return TypedResults.Ok(entity);
    }

    protected async Task<Results<NotFound, NoContent>> DefaultDeleteAsync([FromBody] T entity, IRepository<T> repository)
    {
        var result = await repository.FindAsync(entity.Id);
        if (result == null)
            return TypedResults.NotFound();

        if (DeleteAsync != null)
            await DeleteAsync.InvokeAsync<T>(result);
        else
            await repository.DeleteAsync(result);

        return TypedResults.NoContent();
    }

    protected Delegate? GetAsync;
    protected Delegate? GetAllAsync;
    protected Delegate? CreateAsync;
    protected Delegate? UpdateAsync;
    protected Delegate? DeleteAsync;
}
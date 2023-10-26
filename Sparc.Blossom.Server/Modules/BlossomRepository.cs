using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom;

public interface IBlossomRepository
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public abstract class BlossomRepository<T> : IBlossomRepository where T : Entity
{
    public BlossomRepository()
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

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var queries = GetType().GetMethods(bindingFlags).Where(m => !m.IsSpecialName);
        foreach (var query in queries)
        {
            var factory = RequestDelegateFactory.Create(query);
            AggregateEndpoints.MapGet(query.Name, factory.RequestDelegate).WithName(query.Name).WithOpenApi();
        }


        EntityEndpoints = AggregateEndpoints.MapGroup("{id}");
        EntityEndpoints.AddEndpointFilter<BlossomCommandFilter<T>>();

        var commands = typeof(T).GetMethods(bindingFlags).Where(m => !m.IsSpecialName);
        foreach (var command in commands)
        {
            var factory = RequestDelegateFactory.Create(command, context => (T)context.Items["entity"]!);
            EntityEndpoints.MapPatch(command.Name, factory.RequestDelegate).WithName(command.Name).WithOpenApi();
        }

        //EntityEndpoints.MapGet("", DefaultGetAsync).WithName($"Get{typeof(T).Name}").WithOpenApi();
        //EntityEndpoints.MapPut("", UpdateAsync ?? DefaultUpdateAsync).WithName($"Update{typeof(T).Name}").WithOpenApi();
        //EntityEndpoints.MapDelete("", DeleteAsync ?? DefaultDeleteAsync).WithName($"Delete{typeof(T).Name}").WithOpenApi();
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
        return TypedResults.Created($"{BaseUrl}/{entity.GenericId}", entity);
    }

    protected async Task<Results<NotFound, Ok<T>>> DefaultUpdateAsync([FromBody] T entity, IRepository<T> repository)
    {
        await repository.UpdateAsync(entity);
        return TypedResults.Ok(entity);
    }

    protected async Task<Results<NotFound, NoContent>> DefaultDeleteAsync([FromBody] T entity, IRepository<T> repository)
    {
        var result = await repository.FindAsync(entity.GenericId);
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

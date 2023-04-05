using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

public interface IBlossomAggregate
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public abstract class BlossomAggregate<T> : IBlossomAggregate where T : Entity<string>
{
    public BlossomAggregate()
    {
        GetAllAsync = () => new BlossomGetAllSpecification<T>(100);
    }

    public virtual string Name => typeof(T).Name + "s";

    protected RouteGroupBuilder AggregateEndpoints = null!;
    protected RouteGroupBuilder RootEndpoints = null!;
    protected string BaseUrl => $"/{Name.ToLower()}";

    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapBaseEndpoints(endpoints);
    }

    protected void MapBaseEndpoints(IEndpointRouteBuilder endpoints)
    {
        AggregateEndpoints = endpoints.MapGroup(BaseUrl)
            .WithGroupName(Name);
            //.WithOpenApi();

        AggregateEndpoints.MapGet("", DefaultGetAllAsync);
        AggregateEndpoints.MapPost("", CreateAsync ?? DefaultCreateAsync);

        RootEndpoints = AggregateEndpoints.MapGroup("{id}")
            .AddEndpointFilter<BlossomCommandFilter<T>>();

        RootEndpoints.MapGet("", DefaultGetAsync);
        RootEndpoints.MapPut("", UpdateAsync ?? DefaultUpdateAsync);
        RootEndpoints.MapDelete("", DeleteAsync ?? DefaultDeleteAsync);

        foreach (var command in typeof(T).GetMethods())
        {
            var factory = RequestDelegateFactory.Create(command, context => (T)context.Items["entity"]!, null);
            RootEndpoints.MapPut(command.Name, factory.RequestDelegate);
        }
    }

    protected Ok<T> DefaultGetAsync(T entity)
    {
        return TypedResults.Ok(entity);
    }

    protected async Task<Ok<List<T>>> DefaultGetAllAsync(IRepository<T> repository)
    {
        var results = await repository.GetAllAsync(GetAllAsync());
        return TypedResults.Ok(results);
    }

    protected async Task<Created<T>> DefaultCreateAsync(T entity, IRepository<T> repository)
    {
        await repository.AddAsync(entity);
        return TypedResults.Created($"{BaseUrl}/{entity.Id}", entity);
    }

    protected async Task<Results<NotFound, Ok<T>>> DefaultUpdateAsync(T entity, IRepository<T> repository)
    {
        await repository.UpdateAsync(entity);
        return TypedResults.Ok(entity);
    }

    protected async Task<Results<NotFound, NoContent>> DefaultDeleteAsync(T entity, IRepository<T> repository)
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
    protected Func<ISpecification<T>> GetAllAsync;
    protected Delegate? CreateAsync;
    protected Delegate? UpdateAsync;
    protected Delegate? DeleteAsync;
}

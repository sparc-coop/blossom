using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom;

public abstract class BlossomModule<T, TId, TSafe> where T : Entity<TId> where TId : notnull
{
    public BlossomModule(IRepository<T> repository, IHttpContextAccessor http, string? baseUrl = null)
    {
        Repository = repository;
        Http = http;
        Name = baseUrl?.Trim('/') ?? (typeof(T).Name + "s");
    }

    public IRepository<T> Repository { get; }
    public ClaimsPrincipal User => Http?.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    protected RouteGroupBuilder Endpoints = null!;
    protected string BaseUrl => $"/api/{Name.ToLower()}";
    IHttpContextAccessor Http { get; }
    public string Name { get; set; }

    protected virtual void OnModelCreating(IEndpointRouteBuilder endpoints)
    {
        MapBaseEndpoints(endpoints);
    }

    protected void MapBaseEndpoints(IEndpointRouteBuilder endpoints)
    {
        Endpoints = endpoints.MapGroup(BaseUrl)
            .WithGroupName(Name);
            //.WithOpenApi();

        MapGet("", InternalGetAllAsync);
        MapGet("/{id}", InternalGetAsync);
        MapPost("", InternalCreateAsync);
        MapPut("/{id}", InternalUpdateAsync);
        MapDelete("/{id}", InternalDeleteAsync);
    }

    protected async Task<Ok<IList<TSafe>>> InternalGetAllAsync()
    {
        var results = await Repository.GetAllAsync(GetAllAsync());
        return TypedResults.Ok(Map(results));
    }

    protected async Task<Results<NotFound, Ok<TSafe>>> InternalGetAsync(TId id)
    {
        var result = await GetAsync(id);
        return result == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(Map(result));
    }

    protected async Task<Created<TSafe>> InternalCreateAsync(TSafe entity)
    {
        var root = Map(entity);
        await Repository.AddAsync(root);
        return TypedResults.Created($"{BaseUrl}/{root.Id}", Map(root));
    }

    protected async Task<Results<NotFound, Ok<TSafe>>> InternalUpdateAsync(TId id, TSafe entity)
    {
        var root = Map(entity);
        root.Id = id;
        await Repository.UpdateAsync(root);
        return TypedResults.Ok(Map(root));
    }

    protected async Task<Results<NotFound, NoContent>> InternalDeleteAsync(TId id)
    {
        var result = await Repository.FindAsync(id);
        if (result == null)
            return TypedResults.NotFound();

        await DeleteAsync(result);
        return TypedResults.NoContent();
    }

    protected virtual async Task<T> CreateAsync(TSafe entity)
    {
        var root = Map(entity);
        await Repository.AddAsync(root);
        return root;
    }

    protected virtual async Task<T?> GetAsync(TId id) => await Repository.FindAsync(id);
    protected abstract ISpecification<T> GetAllAsync();
    
    protected virtual async Task DeleteAsync(T result) => await Repository.DeleteAsync(result);

    protected void MapGet(string path, Delegate action) => Endpoints.MapGet(path, action);
    protected void MapPost(string path, Delegate action) => Endpoints.MapPost(path, action);
    protected void MapPut(string path, Delegate action) => Endpoints.MapPut(path, action);
    protected void MapDelete(string path, Delegate action) => Endpoints.MapDelete(path, action);
    protected void MapPost(string path, Action<T> action)
    {
        Endpoints.MapPost("/{id}/" + path, async (TId id) =>
        {
            var room = await GetAsync(id);
            if (room == null)
                return Results.NotFound();

            await Repository.ExecuteAsync(room, action);
            return Results.Ok();
        });
    }

    protected abstract TSafe Map(T entity);
    protected IList<TSafe> Map(IList<T> entities) => entities.Select(Map).ToList();
    protected abstract T Map(TSafe entity);
    protected IList<T> Map(IList<TSafe> entities) => entities.Select(Map).ToList();
}

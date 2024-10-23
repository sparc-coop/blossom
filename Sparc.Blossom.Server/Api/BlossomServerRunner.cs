using Ardalis.Specification;
using Humanizer;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Api;

public interface IBlossomEndpointMapper
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public class BlossomServerRunner<T>(IRepository<T> repository, IRealtimeRepository<T> events, IHttpContextAccessor http) 
    : IRunner<T>, IBlossomEndpointMapper
    where T : BlossomEntity

{
    public string Name => typeof(T).Name.Pluralize();

    public IRepository<T> Repository { get; } = repository;
    public IRealtimeRepository<T> Events { get; } = events;
    protected IHttpContextAccessor Http { get; } = http;
    protected ClaimsPrincipal? User => Http.HttpContext?.User;

    public async Task<T?> GetAsync(object id) => await Repository.FindAsync(id);
    public async Task<IEnumerable<T>> QueryAsync(string? name = null, params object?[] parameters)
    {
        if (name == null)
            return Repository.Query;
        
        // Find the Specification<T> that matches the name
        var assemblyTypes = typeof(T).Assembly.GetTypes();
        var specType = assemblyTypes.FirstOrDefault(x => x.Name == name && x.BaseType == typeof(BlossomQuery<T>))
            ?? throw new Exception($"Specification {name} not found.");

        var spec = (ISpecification<T>)Activator.CreateInstance(specType, parameters)!;
        return await Repository.GetAllAsync(spec);
    }

    public async Task<T> CreateAsync(params object?[] parameters)
    {
        var entity = (T)Activator.CreateInstance(typeof(T), parameters)!;
        await Events.BroadcastAsync(new BlossomEntityAdded<T>(entity));
        // await Repository.AddAsync(entity);
        return entity;
    }

    public async Task ExecuteAsync(object id, string name, params object?[] parameters)
    {
        var entity = await Repository.FindAsync(id)
            ?? throw new Exception($"Entity {id} not found.");

        var action = new Action<T>(x => typeof(T).GetMethod(name)?.Invoke(x, parameters));
        action(entity);
        await Events.BroadcastAsync(name, entity);
        // await Repository.ExecuteAsync(id, action);
    }

    public async Task DeleteAsync(object id)
    {
        var entity = await Repository.FindAsync(id) 
            ?? throw new Exception($"Entity {id} not found.");

        await Events.BroadcastAsync(new BlossomEntityDeleted<T>(entity));
        // await Repository.DeleteAsync(entity);
    }

    public Task OnAsync(object id, string name, params object?[] parameters)
    {
        throw new NotImplementedException();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/{Name.ToLower()}";
        var group = endpoints.MapGroup(baseUrl);
        group.MapGet("{id}", async (IRunner<T> runner, string id) => await runner.GetAsync(id));
        group.MapPost("", async (IRunner<T> runner, object[] parameters) => await runner.CreateAsync(parameters));
        group.MapPost("{name}", async (IRunner<T> runner, string name, object[] parameters) => await runner.QueryAsync(name, parameters));
        group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.ExecuteAsync(id, name, parameters));
        group.MapDelete("{id}", async (IRunner<T> runner, string id) => await runner.DeleteAsync(id));
    }
}

using Ardalis.Specification;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Sparc.Blossom.Data;

public interface IBlossomServerRunner
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public class BlossomServerRunner<T>(IRepository<T> repository, IHttpContextAccessor http) : IRunner<T>, IBlossomServerRunner where T : Entity<string>
{
    public string Name => typeof(T).Name.Pluralize();
    
    public IRepository<T> Repository { get; } = repository;
    protected IHttpContextAccessor Http { get; } = http;
    protected ClaimsPrincipal? User => Http.HttpContext?.User;

    public async Task<T?> GetAsync(object id) => await Repository.FindAsync(id);
    public async Task<IEnumerable<T>> QueryAsync(string name, params object[] parameters)
    {
        // Find the Specification<T> that matches the name
        var specType = typeof(T).Assembly.GetTypes().FirstOrDefault(x => x.Name == name && x.IsAssignableFrom(typeof(ISpecification<T>)))
            ?? throw new Exception($"Specification {name} not found.");

        var spec = (ISpecification<T>)Activator.CreateInstance(specType, parameters)!;
        return await Repository.GetAllAsync(spec);
    }

    public async Task ExecuteAsync(object id, string name, params object[] parameters)
    {
        var action = new Action<T>(x => typeof(T).GetMethod(name)?.Invoke(x, parameters));
        await Repository.ExecuteAsync(id, action);
    }

    public Task OnAsync(object id, string name, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/{Name.ToLower()}";
        var group = endpoints.MapGroup(baseUrl);
        group.MapGet("{id}", async (IRunner<T> runner, string id) => await runner.GetAsync(id));
        group.MapPost("{name}", async (IRunner<T> runner, string name, object[] parameters) => await runner.QueryAsync(name, parameters));
        group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.ExecuteAsync(id, name, parameters));
    }
}
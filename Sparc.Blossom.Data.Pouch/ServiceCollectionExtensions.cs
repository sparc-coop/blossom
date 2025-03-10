using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPouch(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(PouchDbRepository<>));
        return services;
    }

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}

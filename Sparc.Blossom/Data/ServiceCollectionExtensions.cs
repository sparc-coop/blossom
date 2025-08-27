using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Sparc.Blossom.Data.Dexie;

namespace Sparc.Blossom;

public static partial class ServiceCollectionExtensions
{
    public static async Task<IServiceCollection> AddRemoteRepository<T, TResponse>
        (this IServiceCollection services, string url, Func<TResponse, IEnumerable<T>> transformer)
        where T : class
    {
        var results = await BlossomInMemoryRepository<T>.FromUrlAsync(url, transformer);
        services.AddScoped<IRepository<T>>(_ => results);

        return services;
    }

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }

    public static IServiceCollection AddDexie<T>(this IServiceCollection services)
        where T : BlossomEntity<string>
    {
        services.AddSingleton<DexieDatabase>();
        services.AddScoped(typeof(IRepository<T>), typeof(DexieRepository<T>));
        return services;
    }
}

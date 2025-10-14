using Microsoft.JSInterop;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    //public static async Task<IServiceCollection> AddRemoteRepository<T, TResponse>
    //    (this IServiceCollection services, string url, Func<TResponse, IEnumerable<T>> transformer)
    //    where T : class
    //{
    //    var results = await BlossomRepository<T>.FromUrlAsync(url, transformer);
    //    services.AddScoped<IRepository<T>>(_ => );

    //    return services;
    //}

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}

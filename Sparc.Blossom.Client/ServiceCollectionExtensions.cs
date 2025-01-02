using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Refit;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, string baseUrl)
        => services.AddBlossomApi<T>(new Uri(baseUrl));

    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, Uri baseUri, string path)
        => services.AddBlossomApi<T>(new Uri(baseUri, path));

    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, Uri baseUri)
    {
        services.AddRefitClient<IBlossomHttpClient<T>>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUri);

        services.AddTransient<IRunner<T>, BlossomHttpClientRunner<T>>();

        return services;
    }

    public static IServiceCollection AddBlossomApi<T, TSpecificInterface>(this IServiceCollection services, Uri baseUri)
        where TSpecificInterface : IBlossomHttpClient<T>
    {
        services.AddRefitClient(typeof(TSpecificInterface))
            .ConfigureHttpClient(c => c.BaseAddress = baseUri);

        services.AddTransient<IRunner<T>, BlossomHttpClientRunner<T>>();

        return services;
    }

    public static Lazy<Task<IJSObjectReference>> Import(this IJSRuntime js, string module)
    {
        return new(() => js.InvokeAsync<IJSObjectReference>("import", module).AsTask());
    }
}

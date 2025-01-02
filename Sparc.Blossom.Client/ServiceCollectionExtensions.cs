using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, string baseUrl)
        => services.AddBlossomApi<T>(new Uri(baseUrl));

    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, Uri baseUri)
    {
        services.AddRefitClient<IBlossomHttpClient<T>>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUri);

        services.AddTransient<IRunner<T>, BlossomHttpClientRunner<T>>();

        return services;
    }

    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, Uri baseUri, string path)
        => services.AddBlossomApi<T>(new Uri(baseUri, path));
}

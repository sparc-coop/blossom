using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomApi<T>(this IServiceCollection services, string baseUrl)
    {
        services.AddRefitClient<IBlossomHttpClient<T>>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        services.AddTransient<IRunner<T>, BlossomHttpClientRunner<T>>();
        
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoteRepository<T, TResponse>
        (this IServiceCollection services, string url, Func<TResponse, IEnumerable<T>> transformer)
        where T : class
    {
        var results = BlossomInMemoryRepository<T>.FromUrl(url, transformer);
        services.AddScoped<IRepository<T>>(_ => results);

        return services;
    }
}

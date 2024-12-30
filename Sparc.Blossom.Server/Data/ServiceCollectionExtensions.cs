namespace Sparc.Blossom;

public static partial class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
        {
            builder.Services.AddScoped(typeof(IRepository<>), typeof(LocalRepository<>));
        }

        builder.Services.AddScoped(typeof(IRealtimeRepository<>), typeof(BlossomRealtimeRepository<>));
        builder.Services.AddScoped<BlossomHubProxy>();
        return builder;
    }

    public static WebApplicationBuilder AddRemoteRepository<T, TResponse>
        (this WebApplicationBuilder builder, string url, Func<TResponse, IEnumerable<T>> transformer)
        where T : class
    {
        var results = BlossomInMemoryRepository<T>.FromUrl(url, transformer);
        builder.Services.AddScoped<IRepository<T>>(_ => results);

        return builder;
    }
}

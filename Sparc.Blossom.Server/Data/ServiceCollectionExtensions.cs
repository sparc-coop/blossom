namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BlossomSet<>));

        return builder;
    }

    public static WebApplicationBuilder AddBlossomSet<T>(this WebApplicationBuilder builder, IEnumerable<T> items) where T : class
    {
        builder.Services.AddScoped<IRepository<T>>(_ => BlossomSet<T>.FromEnumerable(items));
        return builder;
    }

    public static WebApplicationBuilder AddBlossomSet<TResponse, T>(this WebApplicationBuilder builder, string url, Func<TResponse, IEnumerable<T>> transformer) where T : class
    {
        builder.Services.AddScoped<IRepository<T>>(_ => BlossomSet<T>.FromUrl(url, transformer));
        return builder;
    }
}

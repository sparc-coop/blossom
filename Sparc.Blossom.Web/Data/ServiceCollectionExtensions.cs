namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BlossomSet<>));

        return builder;
    }

    public static WebApplicationBuilder AddRemoteRepository<T, TResponse>
        (this WebApplicationBuilder builder, string url, Func<TResponse, IEnumerable<T>> transformer)
        where T : class
    {
        var results = BlossomSet<T>.FromUrl(url, transformer);
        builder.Services.AddScoped<IRepository<T>>(_ => results);

        return builder;
    }
}

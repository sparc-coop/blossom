﻿using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
        {
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BlossomInMemoryRepository<>));
        }

        builder.Services.AddScoped(typeof(IRealtimeRepository<>), typeof(BlossomRealtimeRepository<>));
        builder.Services.AddScoped<BlossomRealtimeContext>();
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

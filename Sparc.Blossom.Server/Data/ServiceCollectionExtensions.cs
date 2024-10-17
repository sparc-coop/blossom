using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BlossomInMemoryDb<>));

        return builder;
    }

    public static WebApplicationBuilder AddRemoteRepository<T, TResponse>
        (this WebApplicationBuilder builder, string url, Func<TResponse, IEnumerable<T>> transformer)
        where T : class
    {
        var results = BlossomInMemoryDb<T>.FromUrl(url, transformer);
        builder.Services.AddScoped<IRepository<T>>(_ => results);

        return builder;
    }

    public static ModelBuilder EnableRevisions(this ModelBuilder builder)
    {
        var revisionableEntities = builder.Model.GetEntityTypes().Where(x => typeof(IHasRevision).IsAssignableFrom(x.ClrType));

        foreach (var entityType in revisionableEntities)
            builder.Entity(typeof(BlossomRevision<>).MakeGenericType(entityType.ClrType));
        
        return builder;
    }
}

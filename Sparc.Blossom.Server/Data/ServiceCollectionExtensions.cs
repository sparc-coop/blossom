using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRepository(this WebApplicationBuilder builder)
    {
        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
        {
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BlossomInMemoryRepository<>));
            builder.Services.AddScoped(typeof(IEventRepository<>), typeof(BlossomInMemoryRevisionRepository<>)); 
        }

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

    public static EntityTypeBuilder Revisions<T>(this ModelBuilder model) where T : BlossomEntity
    {
        var builder = model.Entity<BlossomRevision<T>>();
        return builder;
    }
}

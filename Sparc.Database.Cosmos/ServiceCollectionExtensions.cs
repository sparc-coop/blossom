using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Sparc.Core;

namespace Sparc.Database.Cosmos;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmos<T>(this IServiceCollection services, string connectionString, string databaseName, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : DbContext
    {
        // Bug fix for Sparc Realtime (events executing in parallel with a scoped context)
        services.AddDbContext<T>(options => options.UseCosmos(connectionString, databaseName, options =>
        {
            options.ConnectionMode(ConnectionMode.Direct);
        }), serviceLifetime);
        services.Add(new ServiceDescriptor(typeof(DbContext), typeof(T), serviceLifetime));
        services.AddTransient(sp => new CosmosDbDatabaseProvider(sp.GetRequiredService<DbContext>(), databaseName));

        services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(CosmosDbRepository<>), serviceLifetime));

        return services;
    }
}

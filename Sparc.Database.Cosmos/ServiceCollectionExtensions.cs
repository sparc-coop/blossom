using Sparc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sparc.Plugins.Database.Cosmos;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmos<T>(this IServiceCollection services, string connectionString, string databaseName) where T : DbContext
    {
        services.AddDbContext<T>(options => options.UseCosmos(connectionString, databaseName, options =>
        {
            options.ConnectionMode(ConnectionMode.Direct);
        }));

        services.AddScoped(typeof(DbContext), typeof(T));
        services.AddScoped(sp => new CosmosDbDatabaseProvider(sp.GetRequiredService<DbContext>(), databaseName));
        services.Replace(ServiceDescriptor.Scoped(typeof(IRepository<>), typeof(CosmosDbRepository<>)));
        services.AddScoped(typeof(ISqlRepository<>), typeof(CosmosDbRepository<>));
        
        return services;
    }
}

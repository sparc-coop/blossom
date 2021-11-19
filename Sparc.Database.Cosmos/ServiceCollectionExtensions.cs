using Sparc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;

namespace Sparc.Plugins.Database.Cosmos
{
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
            services.AddScoped(typeof(IRepository<>), typeof(CosmosDbRepository<>));
            return services;
        }
    }
}

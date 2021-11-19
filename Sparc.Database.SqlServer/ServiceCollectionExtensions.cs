using Sparc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Database.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlServer<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddSqlServerWithoutRepository<T>(connectionString);
            services.AddScoped(typeof(DbContext), typeof(T));
            services.AddScoped(typeof(IRepository<>), typeof(SqlServerRepository<>));
            return services;
        }

        public static IServiceCollection AddSqlServerWithoutRepository<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddDbContext<T>(options => options.UseSqlServer(connectionString));
            return services;
        }
    }
}

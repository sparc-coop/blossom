﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServer<T>(this IServiceCollection services, string connectionString, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : DbContext
    {
        services.AddSqlServerWithoutRepository<T>(connectionString, serviceLifetime);
        services.Add(new ServiceDescriptor(typeof(DbContext), typeof(T), serviceLifetime));

        foreach (var entity in AppDomain.CurrentDomain.GetEntities())
        {
            var repositoryType = typeof(SqlServerRepository<,>).MakeGenericType(typeof(T), entity);
            services.Add(new ServiceDescriptor(typeof(IRepository<>).MakeGenericType(entity), repositoryType, serviceLifetime));
        }
        //services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(SqlServerRepository<>), serviceLifetime));
        return services;
    }

    public static IServiceCollection AddSqlServerWithoutRepository<T>(this IServiceCollection services, string connectionString, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : DbContext
    {
        services.AddDbContextFactory<T>(options => options.UseSqlServer(connectionString));
        return services;
    }
}

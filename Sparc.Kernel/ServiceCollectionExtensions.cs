using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using System.Reflection;
using Sparc.Core;

namespace Sparc.Kernel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection Sparcify<T>(this IServiceCollection services, string? clientUrl = null)
    {
        services.AddControllers(); // for API
        services.AddSingleton<FeatureRouteTransformer>(); // is this necessary? yes

        if (clientUrl != null)
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => 
                builder.WithOrigins(clientUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(x => true)
                .AllowCredentials());
            });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = typeof(T).Namespace ?? typeof(T).Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title, Version = "v1" });
            c.MapType(typeof(IFormFile), () => new OpenApiSchema { Type = "file", Format = "binary" });
            c.UseAllOfToExtendReferenceSchemas();
            c.EnableAnnotations();
        });

        services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

        return services;
    }

    public static IApplicationBuilder Sparcify<T>(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{typeof(T).Namespace ?? typeof(T).Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title} v1"));
        }
        else
        {
            app.UseHsts();
        }

        app.UseExceptionHandler(x => x.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            await context.Response.WriteAsJsonAsync(exception);
        }));

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseCors();

        app.UseRouting();
        app.UseAuthorization();
        app.UseCookiePolicy();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDynamicControllerRoute<FeatureRouteTransformer>("{namespace}/{controller}");
            endpoints.MapRazorPages();
        });
        
        return app;
    }

    public static IServiceCollection AddTransientForBaseType<T>(this IServiceCollection services)
    {
        Type[] features = GetFeatures<T>();

        foreach (var feature in features)
            services.AddTransient(feature);

        return services;
    }

    public static IServiceCollection AddScopedForBaseType<T>(this IServiceCollection services)
    {
        Type[] features = GetFeatures<T>();

        foreach (var feature in features)
            services.AddScoped(feature);

        return services;
    }

    private static Type[] GetFeatures<T>()
    {
        return (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(T).IsAssignableFrom(assemblyType)
                && !assemblyType.IsAbstract
                select assemblyType).ToArray();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;
using System.Reflection;
using Sparc.Core;
using Sparc.Features.Authentication;

namespace Sparc.Features
{
    public static class ServiceCollectionExtensions
    {
        private static string? AuthenticationServerUrl;

        public static IServiceCollection Sparcify<T>(this IServiceCollection services, string? clientUrl = null)
        {
            services.AddControllers(); // for API
            services.AddSingleton<FeatureRouteTransformer>(); // is this necessary? yes
            services.AddMediatR(typeof(T)); // For domain events
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

                // Add JWT Authentication
                c.OperationFilter<SwaggerAuthorizeFilter>();
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = "bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
            });

            if (!services.Any(x => x.ServiceType == typeof(IRepository<>)))
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
}

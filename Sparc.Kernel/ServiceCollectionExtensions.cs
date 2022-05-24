using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Sparc.Core;
using Sparc.Kernel.Authentication;

namespace Sparc.Kernel;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder Sparcify(this WebApplicationBuilder builder, string? clientUrl = null)
    {
        builder.Services.AddControllers(); // for API
        builder.Services.AddSingleton<FeatureRouteTransformer>(); // is this necessary? yes

        if (clientUrl != null)
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                builder.WithOrigins(clientUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(x => true)
                .AllowCredentials());
            });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = builder.Environment.ApplicationName, Version = "v1" });
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

        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            builder.Services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));
        
        builder.Services.AddSingleton<RootScope>();

        return builder;
    }

    public static WebApplication Sparcify(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1"));
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
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Sparc.Blossom.Data;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Server;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossom(this WebApplicationBuilder builder, IComponentRenderMode? renderMode = null, string? clientUrl = null)
    {
        var razor = builder.Services.AddRazorComponents();
        if (renderMode == RenderMode.Server || renderMode == RenderMode.Auto)
            razor.AddServerComponents();
        if (renderMode == RenderMode.WebAssembly || renderMode == RenderMode.Auto)
            razor.AddWebAssemblyComponents();

        //builder.Services.AddGrpc().AddJsonTranscoding();
        //builder.Services.AddGrpcSwagger();
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

        builder.Services.RegisterAggregates();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        if (!builder.Services.Any(x => x.ServiceType == typeof(IRepository<>)))
            builder.Services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

        //builder.Services.AddRazorPages();
        //builder.Services.AddHttpContextAccessor();
        builder.Services.AddOutputCache();
        builder.Services.AddSingleton<AdditionalAssembliesProvider>();

        return builder;
    }

    public static WebApplication UseBlossom<T>(this WebApplicationBuilder builder, params System.Reflection.Assembly[] additionalAssemblies)
    {
        var app = builder.Build();

        app.UseBlossom();

        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            if (builder.IsWebAssembly())
                app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        var razor = app.MapRazorComponents<T>();

        if (additionalAssemblies?.Length > 0)
        {
            razor.AddAdditionalAssemblies(additionalAssemblies);
            app.Services.GetRequiredService<AdditionalAssembliesProvider>().Assemblies = additionalAssemblies;
        }

        if (builder.IsServer())
            razor.AddServerRenderMode();

        if (builder.IsWebAssembly())
            razor.AddWebAssemblyRenderMode();

        return app;
    }

    public static bool IsWebAssembly(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ServiceType.Name.Contains("WebAssemblyEndpointProvider"));

    public static bool IsServer(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ServiceType.Name.Contains("CircuitEndpointProvider"));

    public static WebApplication UseBlossom(this WebApplication app)
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

        app.UseCookiePolicy();
        app.MapAggregates();

        app.UseExceptionHandler(x => x.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            await context.Response.WriteAsJsonAsync(exception);
        }));

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseCors();

        app.UseOutputCache();
        app.UseAuthorization();

        app.MapRazorPages();

        return app;
    }

    public static IApplicationBuilder UseCultures(this IApplicationBuilder app, string[] supportedCultures)
    {
        app.UseRequestLocalization(options => options
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures));

        return app;
    }
    public static IApplicationBuilder UseAllCultures(this IApplicationBuilder app)
    {
        var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(x => x.Name)
            .ToArray();
        
        app.UseCultures(allCultures);

        return app;
    }
   
}

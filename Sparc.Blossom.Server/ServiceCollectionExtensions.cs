using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sparc.Blossom.Data;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Server;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossom<T>(this WebApplicationBuilder builder, IComponentRenderMode? renderMode = null, string? clientUrl = null)
    {
        var razor = builder.Services.AddRazorComponents();
        renderMode ??= RenderMode.InteractiveServer;

        if (renderMode == RenderMode.InteractiveServer || renderMode == RenderMode.InteractiveAuto)
            razor.AddInteractiveServerComponents();
        //if (renderMode == RenderMode.InteractiveWebAssembly || renderMode == RenderMode.InteractiveAuto)
        //    razor.AddInteractiveWebAssemblyComponents();

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

        builder.Services.RegisterAggregates<T>();
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

        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1"));

            //if (builder.IsWebAssembly())
            //    app.UseWebAssemblyDebugging();
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
            razor.AddInteractiveServerRenderMode();

        //if (builder.IsWebAssembly())
        //    razor.AddInteractiveWebAssemblyRenderMode();

        app.MapAggregates<T>();

        return app;
    }

    public static bool IsWebAssembly(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ImplementationType?.Name.Contains("WebAssemblyEndpointProvider") == true);

    public static bool IsServer(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ImplementationType?.Name.Contains("CircuitEndpointProvider") == true);

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

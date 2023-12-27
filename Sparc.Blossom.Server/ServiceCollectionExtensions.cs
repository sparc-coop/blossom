using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sparc.Blossom.Data;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Server;
using Sparc.Blossom.Api;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossom<T, TUser>(this WebApplicationBuilder builder, IComponentRenderMode? renderMode = null) where TUser : BlossomUser, new() where T : Entity
    {
        var razor = builder.Services.AddRazorComponents();
        renderMode ??= RenderMode.InteractiveAuto;

        if (renderMode == RenderMode.InteractiveServer || renderMode == RenderMode.InteractiveAuto)
            razor.AddInteractiveServerComponents();
        if (renderMode == RenderMode.InteractiveWebAssembly || renderMode == RenderMode.InteractiveAuto)
            razor.AddInteractiveWebAssemblyComponents();

        builder.AddBlossomAuthentication<TUser>();
        builder.Services.AddBlossomContexts<T>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.AddBlossomRepository();

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

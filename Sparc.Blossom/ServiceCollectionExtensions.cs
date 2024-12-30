using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;
using System.Reflection;

namespace Sparc.Blossom;

public static partial class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossom(this WebApplicationBuilder builder, Action<WebApplicationBuilder>? options = null, IComponentRenderMode? renderMode = null)
    {
        builder.AddBlossom<BlossomUser>();
        return builder;

    }

    public static WebApplicationBuilder AddBlossom<TUser>(this WebApplicationBuilder builder, Action<WebApplicationBuilder>? options = null, IComponentRenderMode? renderMode = null, Assembly? apiAssembly = null)
        where TUser : BlossomUser, new()
    {
        var razor = builder.Services.AddRazorComponents();
        renderMode ??= RenderMode.InteractiveAuto;

        if (renderMode is InteractiveServerRenderMode || renderMode is InteractiveAutoRenderMode)
            razor.AddInteractiveServerComponents();
        //if (renderMode is InteractiveWebAssemblyRenderMode || renderMode is InteractiveAutoRenderMode)
        //    razor.AddInteractiveWebAssemblyComponents();

        builder.AddBlossomAuthentication<TUser>();

        options?.Invoke(builder);

        builder.RegisterBlossomContexts(apiAssembly);

        builder.Services.AddEndpointsApiExplorer();

        builder.AddBlossomRepository();

        return builder;
    }

    public static WebApplication UseBlossom<T>(this WebApplicationBuilder builder)
    {
        builder.Services.AddServerSideBlazor();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddOutputCache();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            //if (builder.IsWebAssembly())
            //app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.MapStaticAssets();
        app.UseAntiforgery();

        app.UseBlossomAuthentication();

        if (builder.Services.Any(x => x.ServiceType.Name.Contains("Kori")))
            app.UseAllCultures();

        var razor = app.MapRazorComponents<T>();

        if (builder.IsServer())
            razor.AddInteractiveServerRenderMode();

        //if (builder.IsWebAssembly())
        //    razor.AddInteractiveWebAssemblyRenderMode();

        var server = Assembly.GetEntryAssembly();
        var client = typeof(T).Assembly;
        if (server != null && server != client)
            razor.AddAdditionalAssemblies(server);

        app.MapBlossomContexts(server!);

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

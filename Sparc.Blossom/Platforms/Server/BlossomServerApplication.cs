using Microsoft.AspNetCore.Components;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using System.Globalization;
using System.Reflection;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomServerApplication : IBlossomApplication
{
    public WebApplicationBuilder Builder { get; }
    public WebApplication Host { get; set; }
    public IServiceProvider Services => Host.Services;
    public bool IsDevelopment => Builder.Environment.IsDevelopment();

    public BlossomServerApplication(WebApplicationBuilder builder)
    {
        Builder = builder;

        Host = builder.Build();

        if (Builder.Environment.IsDevelopment())
            Host.UseDeveloperExceptionPage();
        else
        {
            Host.UseExceptionHandler("/Error", createScopeForErrors: true);
            Host.UseHsts();
        }

        Host.UseHttpsRedirection();
        Host.MapStaticAssets();
        Host.UseAntiforgery();

        UseBlossomAuthentication();

        if (IsDevelopment)
            Host.MapScalarApiReference();

        if (Builder.Services.Any(x => x.ServiceType.Name.Contains("Kori")))
            UseAllCultures();
    }

    public async Task RunAsync()
    {
        await Host.RunAsync();
    }

    public async Task RunAsync<TApp>()
    {
        var razor = Host.MapRazorComponents<TApp>();

        if (Builder.Services.Any(x => x.ImplementationType?.Name.Contains("CircuitEndpointProvider") == true))
            razor.AddInteractiveServerRenderMode();

        if (Builder.Services.Any(x => x.ImplementationType?.Name.Contains("WebAssemblyEndpointProvider") == true))
            razor.AddInteractiveWebAssemblyRenderMode();

        var server = Assembly.GetEntryAssembly();
        var client = typeof(TApp).Assembly;
        if (server != null && server != client)
            razor.AddAdditionalAssemblies(server);

        MapBlossomContexts(server!);

        Host.MapHub<BlossomHub>("/_realtime");

        await RunAsync();
    }

    void UseBlossomAuthentication()
    {
        Host.UseCookiePolicy(new() { MinimumSameSitePolicy = SameSiteMode.Strict });
        Host.UseAuthentication();
        Host.UseAuthorization();
        Host.UseMiddleware<BlossomAuthenticatorMiddleware>();
    }

    void UseCultures(string[] supportedCultures)
    {
        Host.UseRequestLocalization(options => options
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures));
    }

    void UseAllCultures()
    {
        var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(x => x.Name)
            .ToArray();

        UseCultures(allCultures);
    }

    void MapBlossomContexts(Assembly assembly)
    {
        var mapper = new BlossomEndpointMapper(assembly);
        mapper.MapEntityEndpoints(Host);
    }
}

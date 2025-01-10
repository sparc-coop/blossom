using Scalar.AspNetCore;
using System.Globalization;
using System.Linq.Expressions;
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
        {
            Host.MapOpenApi();
            Host.MapScalarApiReference();
        }

        if (Builder.Services.Any(x => x.ServiceType.Name.Contains("Kori")))
            UseAllCultures();
    }

    public async Task RunAsync()
    {
        MapBlossomContexts(Assembly.GetEntryAssembly()!);
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

        Host.MapHub<BlossomHub>("/_realtime", options =>
        {
            options.AllowStatefulReconnects = true;
        });

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
        var dtos = assembly.GetDtos();
        foreach (var dto in dtos)
        {
            GetType().GetMethod("MapEndpoints")!.MakeGenericMethod(dto.Key, dto.Value).Invoke(this, [assembly]);
        }
    }

    public void MapEndpoints<T, TEntity>(Assembly assembly)
    {
        var aggregateProxy = assembly.GetAggregateProxy(typeof(T));

        var name = aggregateProxy?.Name.ToLower() ?? typeof(TEntity).Name.ToLower();
        var baseUrl = $"/{name}";

        var group = Host.MapGroup(baseUrl);
        group.MapGet("{id}", async (IRunner<T> runner, string id) => await runner.Get(id));
        group.MapPost("", async (IRunner<T> runner, object[] parameters) => await runner.Create(parameters));
        group.MapPost("_queries/{name}", async (IRunner<T> runner, string name, object[] parameters) => await runner.ExecuteQuery<object?>(name, parameters));
        group.MapPut("{id}/{name}", async (IRunner<T> runner, string id, string name, object[] parameters) => await runner.Execute(id, name, parameters));
        group.MapPost("_undo", async (IRunner<T> runner, string id, long? revision) => await runner.Undo(id, revision));
        group.MapPost("_redo", async (IRunner<T> runner, string id, long? revision) => await runner.Redo(id, revision));
        group.MapGet("_metadata", async (IRunner<T> runner) => await runner.Metadata());
        group.MapPatch("{id}", async (IRunner<T> runner, string id, BlossomPatch patch) => await runner.Patch(id, patch));
        //group.MapPost("_queries", async (IRunner<T> runner, string name, BlossomQueryOptions options, object[] parameters) => await runner.ExecuteQuery(name, options, parameters));
        group.MapDelete("{id}", async (IRunner<T> runner, string id) => await runner.Delete(id));

        //if (aggregateProxy != null)
        //{
        //    var aggregateMethods = aggregateProxy.GetMyMethods();
        //    foreach (var method in aggregateMethods)
        //    {
        //        var proxyParameter = Expression.Parameter(aggregateProxy, aggregateProxy.Name.ToLower());
        //        var methodParameters = method.GetParameters().Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
        //        var allParameters = new[] { proxyParameter }.Concat(methodParameters).ToArray();

        //        var lambda = Expression.Lambda(
        //            Expression.Call(proxyParameter, method, methodParameters),
        //            allParameters.Select(p => Expression.Parameter(p.Type, p.Name)).ToArray()
        //        );
        //        var compiled = lambda.Compile()
        //        group.MapPost(method.Name, compiled);
        //    }
        //}

        //var entityMethods = typeof(T).GetMyMethods();
        //foreach (var method in entityMethods)
        //{
        //}
    }

}

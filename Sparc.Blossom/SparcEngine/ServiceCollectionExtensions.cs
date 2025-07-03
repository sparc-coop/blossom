using Refit;

namespace Sparc.Engine;

public static class ServiceCollectionExtensions
{
    public static void AddSparcEngine(this IServiceCollection services, Uri? uri = null)
    {
        uri ??= new Uri("https://engine.sparc.coop");
        
        services.AddRefitClient<ISparcEngine>()
            .ConfigureHttpClient(x => x.BaseAddress = uri)
            .AddStandardResilienceHandler();
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlossomAnimation(this IServiceCollection services)
    {
        services.AddScoped<BlossomAnimator>();
        return services;
    }
}

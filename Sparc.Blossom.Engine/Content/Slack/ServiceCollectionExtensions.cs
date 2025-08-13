using Microsoft.Extensions.Options;

namespace Sparc.Blossom.Content.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackIntegration(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SlackIntegrationOptions>(config.GetSection("SlackIntegration"));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<SlackIntegrationOptions>>().Value);
        services.AddSingleton<ISlackIntegrationService, SlackIntegrationService>();
        return services;
    }
}

namespace Sparc.Blossom.Plugins.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackIntegration(this IServiceCollection services)
    {
        services.AddSingleton<SlackIntegrationService>();
        return services;
    }
}

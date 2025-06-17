using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid.Extensions.DependencyInjection;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration configuration, string sectionName = "Twilio")
    {
        var twilioConfig = configuration.GetSection(sectionName).Get<TwilioConfiguration>()!;
        if (string.IsNullOrWhiteSpace(twilioConfig.AuthToken))
            twilioConfig.AuthToken = configuration.GetConnectionString("Twilio")
                ?? throw new InvalidOperationException("Twilio Auth Token is not configured.");

        services.AddSingleton(_ => twilioConfig).AddScoped<TwilioService>();

        var sendGridApiKey = configuration.GetConnectionString("SendGrid")
            ?? twilioConfig.SendGridApiKey;
        if (!string.IsNullOrWhiteSpace(sendGridApiKey))
            services.AddSendGrid(options =>
            {
                options.ApiKey = sendGridApiKey;
            });
        
        return services;
    }

}

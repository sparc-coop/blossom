using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid.Extensions.DependencyInjection;

namespace Sparc.Notifications.Twilio
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration configuration, string sectionName = "Twilio")
        {
            var twilioConfig = configuration.GetSection(sectionName).Get<TwilioConfiguration>();
            services.AddSingleton(_ => twilioConfig).AddScoped<TwilioService>();

            if (!string.IsNullOrWhiteSpace(twilioConfig.SendGridApiKey))
                services.AddSendGrid(options =>
                {
                    options.ApiKey = twilioConfig.SendGridApiKey;
                });
            
            return services;
        }

    }
}

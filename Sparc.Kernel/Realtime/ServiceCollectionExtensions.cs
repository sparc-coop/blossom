using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sparc.Blossom;
using System.Text.Json.Serialization;

namespace Sparc.Realtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSparcRealtime<THub>(this IServiceCollection services, string hubName = "hub") where THub : SparcHub
    {
        services.AddSwaggerGen(options =>
        {
            options.DocumentFilter<PolymorphismDocumentFilter<Notification, THub>>();
            options.SchemaFilter<PolymorphismSchemaFilter<Notification, THub>>();
        });

        var signalR = services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        //.AddMessagePackProtocol();

        services.AddMediatR(typeof(THub).Assembly, typeof(Notification).Assembly, typeof(SparcNotificationForwarder<>).Assembly);
        services.AddSingleton<Publisher>();

        // Use the User ID as the SignalR user identifier    
        services.AddSingleton<IUserIdProvider, UserIdProvider>();
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(_ => new SparcHubAuthenticator(hubName));

        services.AddTransient<IHubContext<SparcHub>>(s => s.GetRequiredService<IHubContext<THub>>());

        return services;
    }
}

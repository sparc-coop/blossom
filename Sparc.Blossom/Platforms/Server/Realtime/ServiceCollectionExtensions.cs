using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRealtime(this WebApplicationBuilder builder, AppDomain? domain = null)
    {
        domain ??= AppDomain.CurrentDomain;
        builder.Services.AddBlossomRealtime(domain)
            .AddHostedService<BlossomJobProcessor>()
            .AddHostedService<BlossomChannelProcessor>();
        
        return builder;
    }
}

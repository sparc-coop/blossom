using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sparc.Blossom.Realtime;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomRealtime(this WebApplicationBuilder builder, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        builder.Services.AddBlossomRealtime(assembly);
        builder.Services.AddHostedService<BlossomJobProcessor>();
        return builder;
    }
}

using System.Reflection;

namespace Sparc.Blossom.Api;

public static class ServiceCollectionExtensions
{
    public static void MapBlossomContexts(this WebApplication app, Assembly assembly)
    {
        var mapper = new BlossomEndpointMapper(assembly);
        mapper.MapEntityEndpoints(app);
    }
}

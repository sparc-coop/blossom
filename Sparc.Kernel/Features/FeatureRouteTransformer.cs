using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Sparc.Kernel;

public class FeatureRouteTransformer : DynamicRouteValueTransformer
{
    public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        if (!values.ContainsKey("namespace") || !values.ContainsKey("controller"))
            return ValueTask.FromResult(values);

        var controller = values["namespace"] as string;
        var action = values["controller"] as string; 

        return ValueTask.FromResult(values);
    }
}

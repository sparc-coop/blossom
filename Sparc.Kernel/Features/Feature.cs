using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sparc.Kernel;

[Feature]
public abstract class Feature<TOut>
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task<TOut> ExecuteAsync();

    [HttpPost("")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public override async Task<ActionResult<TOut>> HandleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteAsync();
            return Ok(result);
        }
        catch (HttpResponseException e)
        {
            return this.Exception(e);
        }
    }
}

public class FeatureAttribute : RouteAttribute
{
    public FeatureAttribute() : base($"api/[controller]")
    {
    }
}

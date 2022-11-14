using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sparc.Kernel;

[PublicFeature]
public abstract class PublicFeature<T, TOut> : EndpointBaseAsync.WithRequest<T>.WithActionResult<TOut>
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task<TOut> ExecuteAsync(T request);

    [HttpPost("")]
    [Authorize]
    [AllowAnonymous]
    public override async Task<ActionResult<TOut>> HandleAsync([FromBody] T request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteAsync(request);
            return Ok(result);
        }
        catch (HttpResponseException e)
        {
            return this.Exception(e);
        }
    }
}

[PublicFeature]
public abstract class PublicFeature<TOut> : EndpointBaseAsync.WithoutRequest.WithActionResult<TOut>
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task<TOut> ExecuteAsync();

    [HttpPost("")]
    [Authorize]
    [AllowAnonymous]
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

public class PublicFeatureAttribute : RouteAttribute
{
    public PublicFeatureAttribute() : base($"publicapi/[controller]")
    {
    }
}


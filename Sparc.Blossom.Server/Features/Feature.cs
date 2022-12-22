using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sparc.Blossom;

[Feature]
public abstract class Feature<T, TOut> : EndpointBaseAsync.WithRequest<T>.WithActionResult<TOut>
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task<TOut> ExecuteAsync(T request);

    [HttpPost("")]
    [Authorize]
    public override async Task<ActionResult<TOut>> HandleAsync([FromBody]T request, CancellationToken cancellationToken)
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

[Feature]
public abstract class Feature<TOut> : EndpointBaseAsync.WithoutRequest.WithActionResult<TOut>
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task<TOut> ExecuteAsync();

    [HttpPost("")]
    [Authorize]
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

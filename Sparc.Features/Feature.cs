using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Sparc.Features
{

    [Feature]
    public abstract class Feature<T, TOut> : BaseAsyncEndpoint.WithRequest<T>.WithResponse<TOut>
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        public abstract Task<TOut> ExecuteAsync(T request);

        [HttpPost("")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
    public abstract class Feature<TOut> : BaseAsyncEndpoint.WithoutRequest.WithResponse<TOut>
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
}

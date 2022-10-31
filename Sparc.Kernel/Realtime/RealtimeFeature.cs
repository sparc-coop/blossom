using Ardalis.ApiEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sparc.Realtime;

[RealtimeFeature]
public abstract class RealtimeFeature<T> : EndpointBaseAsync.WithRequest<T>.WithoutResult, INotificationHandler<T> where T : SparcNotification
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract Task ExecuteAsync(T item);

    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task Handle(T request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request);
    }

    [HttpPost("")]
    [Authorize]
    public override async Task<ActionResult> HandleAsync(T request, CancellationToken cancellationToken = default)
    {
        await Handle(request, cancellationToken);
        return Ok();
    }
}

public class RealtimeFeatureAttribute : RouteAttribute
{
    public RealtimeFeatureAttribute() : base($"events/[controller]")
    {
    }
}

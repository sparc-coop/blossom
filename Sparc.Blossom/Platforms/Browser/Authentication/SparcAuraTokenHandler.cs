using System.Net.Http.Headers;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class SparcAuraBrowserTokenHandler(ClaimsPrincipal principal) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (principal == null)
            return await base.SendAsync(request, cancellationToken);

        var token = principal.Get("sparcaura-access");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}

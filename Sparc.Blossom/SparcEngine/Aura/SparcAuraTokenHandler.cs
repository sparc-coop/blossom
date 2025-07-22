using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Authentication;
using System.Net.Http.Headers;

namespace Sparc.Engine.Aura;

public class SparcAuraTokenHandler(IHttpContextAccessor http) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var principal = http.HttpContext?.User;
        if (principal == null)
            return await base.SendAsync(request, cancellationToken);

        var token = principal.Get("sparcaura-access");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Forward accept-language header from incoming request if present
        var acceptLanguage = http.HttpContext?.Request.Headers.AcceptLanguage;
        if (!request.Headers.Contains("Accept-Language") && !string.IsNullOrWhiteSpace(acceptLanguage))
            request.Headers.Add("Accept-Language", acceptLanguage.ToString());

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}

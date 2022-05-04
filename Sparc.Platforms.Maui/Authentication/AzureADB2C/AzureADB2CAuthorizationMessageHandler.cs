using Microsoft.Identity.Client;
using Sparc.Core;

namespace Sparc.Platforms.Maui;

public class SparcAuthorizationMessageHandler : DelegatingHandler
{
    public SparcAuthorizationMessageHandler(ISparcAuthenticator authenticator)
    {
        Authenticator = authenticator;
    }

    public ISparcAuthenticator Authenticator { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Authenticator.LoginAsync(request.RequestUri.AbsoluteUri);
            var accessToken = result.FindFirst("access_token")?.Value;
            if (accessToken != null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
        catch (MsalException ex)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(ex.Message)
            };
            return response;
        }
    }
}

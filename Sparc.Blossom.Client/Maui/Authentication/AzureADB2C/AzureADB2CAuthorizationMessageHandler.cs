using Microsoft.Identity.Client;
using Sparc.Blossom;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthorizationMessageHandler : DelegatingHandler
{
    public BlossomAuthorizationMessageHandler(IAuthenticator authenticator)
    {
        Authenticator = authenticator;
    }

    public IAuthenticator Authenticator { get; }

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

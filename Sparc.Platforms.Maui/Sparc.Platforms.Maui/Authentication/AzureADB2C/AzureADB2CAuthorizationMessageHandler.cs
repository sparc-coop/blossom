using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Client;
using Sparc.Core;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui
{
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

    public class InsecureSparcAuthorizationMessageHandler : SelfHostedAuthorizationMessageHandler
    {
        public InsecureSparcAuthorizationMessageHandler(AuthenticationStateProvider provider) : base(provider)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (InnerHandler is HttpClientHandler httpclienthandler)
                httpclienthandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (cert.Issuer.Equals("CN=localhost"))
                        return true;
                    return errors == System.Net.Security.SslPolicyErrors.None;
                };

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

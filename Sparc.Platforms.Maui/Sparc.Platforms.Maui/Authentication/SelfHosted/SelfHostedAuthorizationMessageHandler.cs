using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Maui.Essentials;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui
{
    public class SelfHostedAuthorizationMessageHandler : DelegatingHandler
    {
        public SelfHostedAuthorizationMessageHandler(AuthenticationStateProvider provider)
        {
            Provider = provider;
        }

        public AuthenticationStateProvider Provider { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthenticationState result;
            try
            {
                result = await Provider.GetAuthenticationStateAsync();
            }
            catch (Exception ex)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(ex.Message)
                };
                return response;
            }

            var storage_token = await SecureStorage.GetAsync("AccessToken");
            var accessToken = result.User?.FindFirst("access_token");
            
            if (accessToken != null || storage_token != null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken?.Value ?? storage_token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

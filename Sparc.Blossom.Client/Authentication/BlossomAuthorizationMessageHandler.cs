using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthorizationMessageHandler : DelegatingHandler, IDisposable
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        //request.Headers.Add("X-CSRF", "1");
        return await base.SendAsync(request, cancellationToken);
    }
}

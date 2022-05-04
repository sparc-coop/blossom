using IdentityModel.OidcClient.Browser;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

// Taken from https://www.davidbritch.com/2020/04/authentication-from-xamarinforms-app_8.html
namespace Sparc.Platforms.Maui;

public class WebAuthenticatorBrowser : IBrowser
{
    public WebAuthenticatorBrowser(string redirectUri)
    {
        RedirectUri = redirectUri;
    }

    public string RedirectUri { get; }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        WebAuthenticatorResult authResult =
                await WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions
                {
                    Url = new Uri(options.StartUrl),
                    CallbackUrl = new Uri(RedirectUri),
                    PrefersEphemeralWebBrowserSession = true
                });
        //(new Uri(options.StartUrl), new Uri(RedirectUri));

        return new BrowserResult
        {
            Response = ParseAuthenticatorResult(authResult)
        };
    }

    string ParseAuthenticatorResult(WebAuthenticatorResult result)
    {
        string code = result?.Properties["code"];
        string scope = result?.Properties["scope"];
        string state = result?.Properties["state"];
        string sessionState = result?.Properties["session_state"];
        return $"{RedirectUri}#code={code}&scope={scope}&state={state}&session_state={sessionState}";
    }
}

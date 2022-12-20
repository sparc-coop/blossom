// Taken from https://www.davidbritch.com/2020/04/authentication-from-xamarinforms-app_8.html
namespace Sparc.Blossom.Authentication;

public class WebAuthenticatorBrowser : IBrowser
{
    public WebAuthenticatorBrowser(string redirectUri)
    {
        RedirectUri = redirectUri;
    }

    public string RedirectUri { get; }

    public async Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
    {
        var authResult =
               await WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions
               {
                   Url = uri,
                   CallbackUrl = new Uri(RedirectUri),
                   PrefersEphemeralWebBrowserSession = true
               });
        //(new Uri(options.StartUrl), new Uri(RedirectUri));

        return authResult.AccessToken != null;
    }
}

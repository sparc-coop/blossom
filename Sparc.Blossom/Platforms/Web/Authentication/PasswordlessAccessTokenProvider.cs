using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Web;

namespace Sparc.Authentication;

public class PasswordlessAccessTokenProvider : IAccessTokenProvider
{
    public static readonly string TokenName = "_sparc_passwordless_access_token";

    public PasswordlessAccessTokenProvider(AuthenticationStateProvider provider, ILocalStorageService localStorage, NavigationManager navigation)
    {
        Provider = provider as IAccessTokenProvider;
        LocalStorage = localStorage;
        Navigation = navigation;
    }

    public IAccessTokenProvider? Provider { get; }
    public ILocalStorageService LocalStorage { get; }
    public NavigationManager Navigation { get; }

    public async Task SetAccessToken()
    {
        var uri = new Uri(Navigation.Uri);

        var queryString = HttpUtility.ParseQueryString(uri.Query);
        if (queryString["passwordless"] != null)
        {
            await LocalStorage.SetItemAsync(TokenName, queryString["passwordless"]);
            Navigation.NavigateTo(queryString["returnUrl"]!);
        }
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        return await RequestAccessTokenWithOptionalOptions();
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        return await RequestAccessTokenWithOptionalOptions(options);
    }

    public async ValueTask<AccessTokenResult> RequestAccessTokenWithOptionalOptions(AccessTokenRequestOptions? options = null)
    {
        // Check local storage for access token
        var token = await LocalStorage.GetItemAsync<string>(TokenName);
        if (token == null)
        {
            if (Provider != null)
                return options == null
                    ? await Provider.RequestAccessToken()
                    : await Provider.RequestAccessToken(options);

            return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect,
                null,
                "/authentication/login",
                new InteractiveRequestOptions
                {
                    Interaction = InteractionType.GetToken,
                    ReturnUrl = options?.ReturnUrl != null ? Navigation.ToAbsoluteUri(options.ReturnUrl).AbsoluteUri : Navigation.Uri,
                    Scopes = options?.Scopes ?? Array.Empty<string>()
                });
        }

        return new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken { Value = token }, null, null);
    }
}

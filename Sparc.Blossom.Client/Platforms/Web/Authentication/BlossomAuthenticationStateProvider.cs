using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticationStateProvider : AuthenticationStateProvider, IAccessTokenProvider
{
    public static readonly string TokenName = "_blossom_access_token";
    private ClaimsPrincipal? _user;

    public BlossomAuthenticationStateProvider(ILocalStorageService localStorage, NavigationManager navigation, IConfiguration config)
    {
        LocalStorage = localStorage;
        Navigation = navigation;
        Config = config;
    }

    public ILocalStorageService LocalStorage { get; }
    public NavigationManager Navigation { get; }
    public IConfiguration Config { get; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_user?.Identity?.IsAuthenticated == true)
            return new AuthenticationState(_user);

        var token = await RequestAccessToken();
        if (token.Status == AccessTokenResultStatus.Success && token.TryGetToken(out var jwt))
        {
            _user = new ClaimsPrincipal(CreateIdentity(jwt.Value));
        }
        else
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_user);
    }

    private static ClaimsIdentity CreateIdentity(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "Blossom", "unique_name", "roles");
        return identity;
    }

    public virtual async Task LoginAsync()
    {
        var uri = new Uri(Navigation.Uri);
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var returnUrl = queryString.AllKeys.Contains("returnUrl") ? queryString["returnUrl"]! : "/";

        if (queryString["token"] != null)
        {
            await LocalStorage.SetItemAsync(TokenName, queryString["token"]);
        }

        var token = await RequestAccessToken();
        if (token.Status == AccessTokenResultStatus.Success)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            Navigation.NavigateTo(returnUrl, true);
        }
        else
        {
            var loginUrl = QueryHelpers.AddQueryString(Config["Blossom:Authority"] + "/_login", "returnUrl", Navigation.Uri);
            Navigation.NavigateTo(loginUrl, true);
        }
    }

    public virtual async Task LogoutAsync()
    {
        await LocalStorage.RemoveItemAsync(TokenName);
        _user = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        Navigation.NavigateToLogout(Config["Blossom:Authority"] + "/_logout");
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
            return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect,
                null,
                "/_authorize",
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

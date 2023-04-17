using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;

namespace Sparc.Authentication;

public class SparcAuthenticator : AuthenticationStateProvider, IAccessTokenProvider
{
    public static readonly string TokenName = "_sparc_access_token";
    private ClaimsPrincipal? _user;

    public SparcAuthenticator(ILocalStorageService localStorage, NavigationManager navigation, IConfiguration config)
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
        var identity = new ClaimsIdentity(jwt.Claims, "Sparc", "unique_name", "roles");
        return identity;
    }

    public virtual async Task LoginAsync(string? returnUrl = null)
    {
        var uri = new Uri(Navigation.Uri);
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        
        if (string.IsNullOrWhiteSpace(returnUrl))       
            returnUrl = queryString.AllKeys.Contains("returnUrl") ? queryString["returnUrl"]! : "/";

        if (queryString["token"] != null)
        {
            await LocalStorage.SetItemAsync(TokenName, queryString["token"]);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            Navigation.NavigateTo(returnUrl, true);
        }
        else
        {
            if (!returnUrl.StartsWith("http"))
                returnUrl = Navigation.ToAbsoluteUri(returnUrl).AbsoluteUri;
            
            var loginUrl = QueryHelpers.AddQueryString(Config["Sparc:Authority"] + "/_login", "returnUrl", returnUrl);
            Navigation.NavigateToLogin(loginUrl);
        }
    }

    public virtual async Task LogoutAsync()
    {
        await LocalStorage.RemoveItemAsync(TokenName);
        _user = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        Navigation.NavigateToLogout(Config["Sparc:Authority"] + "/_logout");
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

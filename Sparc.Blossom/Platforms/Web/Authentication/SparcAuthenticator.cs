using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Json;
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
        if (token.Status == AccessTokenResultStatus.Success && token.TryGetToken(out var jwt) && jwt.Expires > DateTime.UtcNow)
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
        var claims = new List<Claim>();
        var payload = token.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        if (keyValuePairs != null)
        {
            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
        }

        return new ClaimsIdentity(claims, "Sparc");
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public virtual async Task Init()
    {
        var uri = new Uri(Navigation.Uri);
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var returnUrl = queryString.AllKeys.Contains("returnUrl") ? queryString["returnUrl"]! : "/";

        if (queryString["token"] != null)
        {
            await LocalStorage.SetItemAsync(TokenName, queryString["token"]);
            Navigation.NavigateTo(returnUrl);
        }
        else
        {
            var loginUrl = QueryHelpers.AddQueryString(Config["Sparc:Authority"] + "/_login", "returnUrl", Navigation.Uri);
            Navigation.NavigateToLogin(loginUrl);
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

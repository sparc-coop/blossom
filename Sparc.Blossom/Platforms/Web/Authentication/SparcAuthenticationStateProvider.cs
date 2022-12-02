using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace Sparc.Authentication;

public class SparcAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal? _user;

    public SparcAuthenticationStateProvider(IAccessTokenProvider provider)
    {
        Provider = provider;
    }

    public IAccessTokenProvider Provider { get; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_user?.Identity?.IsAuthenticated == true)
            return new AuthenticationState(_user);

        var token = await Provider.RequestAccessToken();
        if (token.Status == AccessTokenResultStatus.Success && token.TryGetToken(out var jwt))
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(jwt.Value), "Sparc"));
        }
        else
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_user);
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string token)
    {
        var claims = new List<Claim>();
        var payload = token.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        if (keyValuePairs == null)
            return claims;

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
        return claims;
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
}

public class SparcAuthenticationStateProvider<TRemoteAuthenticationState, TAccount, TProviderOptions>
    : RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
    public SparcAuthenticationStateProvider(ILocalStorageService localStorage,
        IJSRuntime jsRuntime,
        IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options,
        NavigationManager navigation,
        AccountClaimsPrincipalFactory<TAccount> accountClaimsPrincipalFactory,
        ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>> logger
        ) : base(jsRuntime, options, navigation, accountClaimsPrincipalFactory, logger)
    {
        LocalStorage = localStorage;
    }

    public ILocalStorageService LocalStorage { get; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Try the main auth first
        var result = await base.GetAuthenticationStateAsync();
        if (result.User?.Identity?.IsAuthenticated == true)
            return result;

        // If it fails, try passwordless auth
        var token = await LocalStorage.GetItemAsync<string>(SparcAccessTokenProvider.TokenName);
        if (!string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "Passwordless")));

        // If that fails, use the anonymous user from the main auth
        return result;
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string token)
    {
        var claims = new List<Claim>();
        var payload = token.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        if (keyValuePairs == null)
            return claims;

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
        return claims;
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
}

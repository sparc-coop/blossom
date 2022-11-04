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

public class PasswordlessAuthenticationStateProvider<TRemoteAuthenticationState, TAccount, TProviderOptions>
    : RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
    public PasswordlessAuthenticationStateProvider(ILocalStorageService localStorage,
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
        var token = await LocalStorage.GetItemAsync<string>(PasswordlessAccessTokenProvider.TokenName);
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

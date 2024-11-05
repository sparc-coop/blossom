using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomLegacyAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _user = new(new ClaimsIdentity());
    private static readonly TimeSpan _userCacheRefreshInterval = TimeSpan.FromSeconds(60);
    private DateTimeOffset _userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);

    public BlossomLegacyAuthenticationStateProvider(NavigationManager navigation, BlossomAuthenticationClient client, IConfiguration config)
    {
        Navigation = navigation;
        Config = config;
        Client = client;
    }

    public NavigationManager Navigation { get; }
    public IConfiguration Config { get; }
    public BlossomAuthenticationClient Client { get; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync() 
        => new AuthenticationState(await GetUser(true));

    private async ValueTask<ClaimsPrincipal> GetUser(bool useCache = false)
    {
        var now = DateTimeOffset.Now;
        if (useCache && now < _userLastCheck + _userCacheRefreshInterval)
        {
            return _user;
        }
        _user = await Client.GetUserAsync();
        _userLastCheck = now;
        return _user;
    }

    public virtual Task LoginAsync(bool forceLogin = false)
    {
        if (forceLogin)
            _user = Anonymous();
        
        var loginUrl = $"{Config["Blossom:Authority"]}/_auth/login?returnUrl={Navigation.Uri}";
        Navigation.NavigateTo(loginUrl, true);

        return Task.CompletedTask;
    }

    public virtual Task LogoutAsync()
    {
        _user = Anonymous();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        Navigation.NavigateTo(Config["Blossom:Authority"] + "/_auth/logout", true);

        return Task.CompletedTask;
    }

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

}

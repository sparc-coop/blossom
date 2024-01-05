using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

// This is a client-side AuthenticationStateProvider that determines the user's authentication state by
// looking for data persisted in the page when it was rendered on the server. This authentication state will
// be fixed for the lifetime of the WebAssembly application. So, if the user needs to log in or out, a full
// page reload is required.
//
// This only provides a user name and email for display purposes. It does not actually include any tokens
// that authenticate to the server when making subsequent requests. That works separately using a
// cookie that will be included on HttpClient requests to the server.
public class BlossomAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> authenticationStateTask = defaultUnauthenticatedTask;

    public BlossomAuthenticationStateProvider(PersistentComponentState state, NavigationManager nav)
    {
        Nav = nav;

        if (!state.TryTakeFromJson<BlossomUser>(nameof(BlossomUser), out var userInfo) || userInfo is null)
        {
            return;
        }

        List<Claim> claims = [
            new Claim(ClaimTypes.NameIdentifier, userInfo.Id)
        ];

        if (userInfo.Identity?.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, userInfo.Identity.UserName ?? userInfo.Identity.Email));
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Identity.Email));
        }

        authenticationStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                authenticationType: nameof(BlossomAuthenticationStateProvider)))));
    }

    public NavigationManager Nav { get; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;

    public void Login()
    {
        var loginUrl = $"_auth/login?returnUrl={Uri.EscapeDataString(Nav.Uri)}";
        Nav.NavigateTo(loginUrl, true);
    }
}
